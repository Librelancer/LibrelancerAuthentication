importScripts('./sha256-uint8array.min.js');

function calculateSha256(message) {
    return SHA256.createHash().update(message).digest("hex");
}

onmessage = (e) => {
    var msg = e.data[0];
    var difficulty = e.data[1];
    var nonceInt = 0;
    var data = "";
    console.log("WORKER: Calculating work with difficulty " + difficulty);
    for(const property in msg) {
           data = data.concat(msg[property]);
    }
    var nonce = nonceInt.toString();
    while(calculateSha256(data + nonce).slice(0,difficulty) !== "0".repeat(difficulty)) {
        nonceInt++;
        nonce = nonceInt.toString();
    }
    console.log("WORKER: Calculation complete");
    postMessage({ nonce: nonce, hash: calculateSha256(data + nonce)});
};

