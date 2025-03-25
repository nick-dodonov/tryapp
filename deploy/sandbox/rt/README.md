# RT _(webrtc testing tool)_

Modified version of [Pion ORTC Example](https://github.com/pion/webrtc/tree/master/examples/ortc).
Required for manual investigations of WebRTC DataChannel connectivity in different infra-environments.

And it also helps me to try .go coding a little :smile:. 

* shorter name to simplify testing
* more diagnostics for connectivity offerer/answerer/ice/etc
* allow answer for offerer via stdin (along with existing http way)
* allow STUN setup (to try personal coturn as gateway for stands)
