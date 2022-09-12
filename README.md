# LibrelancerAuthentication

Simple authentication server for the Librelancer game server. It works by providing tokens to clients, which are then passed to the game server. The game server then verifies these tokens to get the associated user and log in.

Tokens are use-once, and are expired by the server once verified or after two minutes - whichever comes first. For security, clients should **not** call these APIs over http:// and only accept encrypted https:// connections.

Passwords are hashed server-side using the ASP.NET password hasher, which at current does salted hashes with the PBKDF2 algorithm.

The location of the user database may be configured in appsettings.json

The web server provides 4 endpoints:

## GET /
Returns a json object containing `{ "application": "authserver", "registerEnabled": "true" }`.
Use this to identify the server.

## POST /register

Accepts JSON object with members `username` and `password`

Returns 200 OK if the user was able to be registered. Returns 400 Bad Request otherwise

## POST /login

Accepts JSON object with members `username` and `password`

Returns 200 OK with `{ "token": "TOKENVALUE" }` on success, this token should be passed to the game server - which will pass it back to the login server. Returns 400 Bad Request on failure

## POST /verifytoken

Accepts JSON object with the member `token`

Returns 200 OK with `{ guid: "accountguid" }` for the account that generated `token` on success, returns 400 Bad Request on failure.


