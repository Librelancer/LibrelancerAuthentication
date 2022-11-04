var registerDifficulty = 0;
var loginDifficulty = 0;
var changePasswordDifficulty = 0; 

//This makes use of sha256-uint8array library
//MIT License
//Copyright (c) 2020-2021 Yusuke Kawasaki

function calculateSha256(message) {
   return SHA256.createHash().update(message).digest("hex");
}

function powMessage(msg, difficulty, done) {
    msg.utctime =  Math.floor((new Date()).getTime() / 1000).toString();
    var nonceInt = 0;
    var data = "";
    for(const property in msg) {
           data = data.concat(msg[property]);
    }
    var nonce = nonceInt.toString();	
    function processChunk()
    {
        var chunkProcess = 0;
        var nextChunk = false;
        while(calculateSha256(data + nonce).slice(0,difficulty) !== "0".repeat(difficulty)) {
            nonceInt++;
            nonce = nonceInt.toString();
            chunkProcess++;
            if(chunkProcess > 9000) {
                nextChunk = true;
                break;
            }
        }
        if(nextChunk) {
            setTimeout(processChunk);
        } else {
            msg.nonce = nonce;
            msg.hash = calculateSha256(data + nonce);
            done();
        }
    }
    setTimeout(processChunk);
}

function doRequest(url, data, difficulty)
{
    showLoader();
    powMessage(data, difficulty, () => {
        fetch(APP_PATH + url,{ method: 'POST', body: JSON.stringify(data), headers: {
            'Content-Type': 'application/json'
        }})
        .then(async (response) => {
          hideLoader();
          if(response.ok)
            alert('Success');
          else
            alert('Error: ' + await response.text());
        })
        .catch((error) => {
          hideLoader();
          alert('Error:' + error);
        });
    })
}

function handleRegister(e) {
    e.preventDefault();
    const formData = new FormData(e.target);
    const formProps = Object.fromEntries(formData);

    if(formProps.confirmpassword !== formProps.password) {
        alert("Passwords do not match");
        return;
    }
    doRequest("/register", {
        username: formProps.username,
        password: formProps.password
    }, registerDifficulty);
}

function handleChangePassword(e) {
    e.preventDefault();
    const formData = new FormData(e.target);
    const formProps = Object.fromEntries(formData);
    doRequest("/changepassword", {
        username: formProps.username,
        oldpassword: formProps.oldpassword,
        newpassword: formProps.newpassword
    }, changePasswordDifficulty);
}

function username(e) {
    var value = e.target.value;
    if(!e.key.match(/[a-zA-Z0-9_]/))
        e.preventDefault();
}

function init()
{
   document.getElementById("register-form").addEventListener("submit",handleRegister);
   document.getElementById("change-password-form").addEventListener("submit",handleChangePassword);
   document.getElementById("server-url").textContent="Server URL: " + APP_PATH;
   fetch(APP_PATH + "/info").then(async (response) => {
       if(response.ok) {
           var res = await response.json();
           console.log(res);
           registerDifficulty = res.registerDifficulty;
           loginDifficulty = res.loginDifficulty;
           changePasswordDifficulty = res.changePasswordDifficulty;
           if(res.registerEnabled) {
              document.getElementById("register-card").className = "";
           } else {
              document.getElementById("register-disabled").className = "";
           }
           document.getElementById("apploading").className = "hidden";
           document.getElementById("container").className = "";
       } else {
           alert("Failed to load application: " + response.text());
       }
   }).catch((x) => alert("Failed to load application: " + x));
}

function hideLoader() {
    document.getElementById("loadingmodal").className = "hidden";
}

function showLoader() {
    document.getElementById("loadingmodal").className = "";
}

window.onload = init;
