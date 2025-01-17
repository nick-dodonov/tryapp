mergeInto(LibraryManager.library, {
    SetupTestCallbackString: function(message, callback) {
        console.log("SetupTestCallbackString:", message);
        var stringMessage = UTF8ToString(message);
        console.log("SetupTestCallbackString:", stringMessage);
        let frameId = 0;
        let timer = setTimeout(function () {
        //let timer = setInterval(() => {
            console.log(">>>> SetupTestCallbackString:", frameId);
            var buffer = stringToNewUTF8(stringMessage); // copy of message because it might be deleted before callback is run
            {{{ makeDynCall('vi', 'callback') }}}(buffer);
            _free(buffer);
            console.log("<<<< SetupTestCallbackString:", frameId);

            ++frameId;
            if (frameId >= 5) {
                console.log("SetupTestCallbackString: cleanup");
                clearInterval(timer);
            }
        }, 1000);
    },
    SetupTestCallbackBytes: function(bytes, size, callback) {
        console.log("SetupTestCallbackBytes1:", bytes, size);
        const data = new Uint8Array(HEAPU8.buffer, bytes, size);
        console.log("SetupTestCallbackBytes2:", data);

        //COPY
        var arrayCopy = new ArrayBuffer(data.byteLength);
        var dataCopy = new Uint8Array(arrayCopy)
        dataCopy.set(data);
        console.log("SetupTestCallbackBytes3:", arrayCopy);
        console.log("SetupTestCallbackBytes4:", dataCopy);

        let frameId = 0;
        let timer = setTimeout(function () {
        //let timer = setInterval(() => {
            console.log(">>>> SetupTestCallbackBytes:", frameId);
            const data = dataCopy; //COPY
            const buffer = _malloc(data.length * data.BYTES_PER_ELEMENT);
            HEAPU8.set(data, buffer);
            {{{ makeDynCall('vi', 'callback') }}}(buffer, data.length);
            _free(buffer);
            console.log("<<<< SetupTestCallbackBytes:", frameId);

            ++frameId;
            if (frameId >= 5) {
                console.log("SetupTestCallbackBytes: cleanup");
                clearInterval(timer);
            }
        }, 1000);
    },

    SetupTestCallbackObj: function(obj, callback) {
        console.log("SetupTestCallbackObj:", typeof(obj), obj);
        let timer = setTimeout(function () {
            console.log("SetupTestCallbackObj: >>>> callback");
            {{{ makeDynCall('vi', 'callback') }}}(obj);
            console.log("SetupTestCallbackObj: <<<< callback");
        }, 1000);
    },
});
