# LibrelancerAuthentication

Simple authentication server for the Librelancer game server

It provides 4 endpoints

## /
Returns a json object containing `{ "application": "authserver", "registerEnabled": "true" }`.
Use this to identify the server.

## /register?username={username}&password={password}

Returns 200 OK if the user was able to be registered. Returns 400 bad request otherwise

## /login?username={username}&password={password}

Returns 200 OK with a json string `{ "token": "TOKENVALUE" }` on success, this token should be passed to the game server - which will pass it back to the login server. Returns 400 bad requests on failure

## /verifytoken?token={token}

Returns the guid for the account that generated `token` on success, returns 400 bad request on failure.


Tokens are use-once, and are expired by the server once verified or after two minutes - whichever comes first. For security, clients should **not** call these APIs over http:// and only accept encrypted https:// connections.

Passwords are hashed server-side using the ASP.NET password hasher, which at current does salted hashes with the PBKDF2 algorithm.

The location of the user database may be configured in appsettings.json

