var registerDifficulty = 0;
var loginDifficulty = 0;
var changePasswordDifficulty = 0; 
var captchaDifficulty = 0;
var solvedCaptchaToken = "";

//This makes use of sha256-uint8array library
//MIT License
//Copyright (c) 2020-2021 Yusuke Kawasaki

function calculateSha256(message) {
   return SHA256.createHash().update(message).digest("hex");
}

function powMessageFallback(msg, difficulty, done)
{
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

function powMessage(msg, difficulty, done) {
    msg.utctime =  Math.floor((new Date()).getTime() / 1000).toString();
    if(window.Worker) {
        myWorker.onmessage = (e) => {
            msg.nonce = e.data.nonce;
            msg.hash = e.data.hash;
            done();
        };
        myWorker.postMessage([msg, difficulty]);
    } else {
        powMessageFallback(msg, difficulty, done);
    }
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

var registerProps = {}
function handleRegister(e) {
    e.preventDefault();
    const formData = new FormData(e.target);
    const formProps = Object.fromEntries(formData);

    if(formProps.confirmpassword !== formProps.password) {
        alert("Passwords do not match");
        return;
    }
    registerProps = formProps;
    loadCaptcha();
}

function continueRegister(token)
{
    doRequest("/register", {
        username: registerProps.username,
        password: registerProps.password,
        captchaToken: token
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

captchaId = "";

function loadCaptcha()
{
    document.getElementById("captchamodal").className = "";
    document.getElementById("captcha-loading").className = "";
    document.getElementById("captcha-background").className = "hidden";
    document.getElementById("captcha-piece").className = "hidden";
    document.getElementById("captchaSlider").className = "hidden";
    var data = {};
    powMessage(data, captchaDifficulty, () => {
       
        fetch(APP_PATH + "/createcaptcha", { method: 'POST', body: JSON.stringify(data), headers: {
            'Content-Type': 'application/json'
        }}).then(async (response) => {
            var res = await response.json();
            document.getElementById("captcha-background").style.backgroundImage = "url('" + res.background + "')";
            document.getElementById("captcha-piece").style.backgroundImage = "url('" + res.piece + "')";
            document.getElementById("captcha-piece").style.marginTop = res.y + "px";
            document.getElementById("captcha-piece").style.marginLeft = "0";
            document.getElementById("captchaSlider").value = "0";
            document.getElementById("captcha-background").className = "";
            document.getElementById("captcha-piece").className = "";
            document.getElementById("captchaSlider").className = "slider";
            document.getElementById("captcha-loading").className = "hidden";
            captchaId = res.id;
        });
    });
}

function slideCaptcha(e)
{
    document.getElementById("captcha-piece").style.marginLeft = e.target.value + "px";
}

function finishCaptcha(e)
{
    document.getElementById("captcha-piece").style.marginLeft = e.target.value + "px";
    var data = {
        'id': captchaId,
        'x': e.target.value
    };
    fetch(APP_PATH + "/checkcaptcha",{ method: 'POST', body: JSON.stringify(data), headers: {
        'Content-Type': 'application/json'
    }}).then(async (response) => {
      if(response.ok) {
        var res = await response.json();
        document.getElementById("captchamodal").className = "hidden";
        continueRegister(res.token);
       }
      else {
        var error = await response.text();
        if(error === "\"Expired\"") loadCaptcha();
        else alert('Error: ' + error);
      }
    });
}

function init()
{
   document.getElementById("register-form").addEventListener("submit",handleRegister);
   document.getElementById("change-password-form").addEventListener("submit",handleChangePassword);
   document.getElementById("server-url").textContent="Server URL: " + APP_PATH;
   document.getElementById("captchaSlider").addEventListener("input",slideCaptcha);
   document.getElementById("captchaSlider").addEventListener("change",finishCaptcha);

   if(window.Worker) {
        myWorker = new Worker('./pow-worker.js');
   }
   fetch(APP_PATH + "/info").then(async (response) => {
       if(response.ok) {
           var res = await response.json();
           console.log(res);
           registerDifficulty = res.registerDifficulty;
           loginDifficulty = res.loginDifficulty;
           changePasswordDifficulty = res.changePasswordDifficulty;
           captchaDifficulty = res.captchaDifficulty;
           if(res.registerEnabled) {
              document.getElementById("register-card").className = "";
           } else {
              document.getElementById("register-disabled").className = "";
           }
           if(res.changePasswordEnabled) {
              document.getElementById("change-password-card").className = "";
           } else {
              document.getElementById("change-password-disabled").className = "";
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
