mergeInto(LibraryManager.library, {
    SendLogToReactNative: function (messagePtr) {
        var message = UTF8ToString(messagePtr);
        // console.log('jslib fun : ' + message);
        if (window.ReactNativeWebView) {
          window.ReactNativeWebView.postMessage(message);
        } 
    },

    SendPostMessage: function(messagePtr) 
    {
      var message = UTF8ToString(messagePtr);
      console.log('SendReactPostMessage, message sent: ' + message);
      if(window.ReactNativeWebView)
      {
        if(message == "authToken"){
          var injectedObjectJson = window.ReactNativeWebView.injectedObjectJson();
          var injectedObj = JSON.parse(injectedObjectJson);

          window.ReactNativeWebView.postMessage('Injected obj : ' + injectedObjectJson);
          
          var combinedData = JSON.stringify({
              socketURL: injectedObj.socketURL.trim(),
              cookie: injectedObj.token.trim(),
              nameSpace: injectedObj.nameSpace ? injectedObj.nameSpace.trim() : ""
          });

          if (typeof SendMessage === 'function') {
            SendMessage('SocketManager', 'ReceiveAuthToken', combinedData);
          }
        }
        window.ReactNativeWebView.postMessage(message);
      }
      else if (typeof window !== "undefined" && window.parent) {
        if (typeof window.parent.postMessage === "function"){
          console.log("Calling window.parent.postMessage");
          window.parent.postMessage({ 
            type: message,
            data: { }
          }, "*");
        }
      }
      else if(window.parent)
      {
        if(message == "authToken")
        {
          window.addEventListener('message', function(event){
            if(event.data.type === 'authToken'){
              var combinedData = JSON.stringify({
                  cookie: event.data.cookie,
                  socketURL: event.data.socketURL,
                  nameSpace: event.data && event.data.nameSpace ? event.data.nameSpace : ''
              }); 

              if (typeof SendMessage === 'function') {
                SendMessage('SocketManager', 'ReceiveAuthToken', combinedData);
              }
              else{
                console.log('SendMessage is not a func');
              }
            }
          });
        }
      }
    },

    RegisterVisibilityChangeListener: function(gameObjectNamePtr) {
      var gameObjectName = UTF8ToString(gameObjectNamePtr);

      function setUnityAudioSuspended(suspended) {
          try {
              var wa = (typeof WEBAudio !== 'undefined') ? WEBAudio
                     : (typeof Module !== 'undefined' && Module.WEBAudio) ? Module.WEBAudio
                     : null;
              if (!wa || !wa.audioContext) return;
              if (suspended) {
                  if (wa.audioContext.state === 'running') wa.audioContext.suspend();
              } else {
                  if (wa.audioContext.state === 'suspended') wa.audioContext.resume();
              }
          } catch (err) { console.warn('[JS] Unity audio suspend/resume failed:', err); }
      }

      function sendFocusToUnity(focused) {
          setUnityAudioSuspended(!focused);
          try {
              var value = focused ? '1' : '0';
              if (typeof SendMessage === 'function') {
                  SendMessage(gameObjectName, 'OnFocusChanged', value);
              } else if (typeof unityInstance !== 'undefined' && unityInstance && unityInstance.SendMessage) {
                  unityInstance.SendMessage(gameObjectName, 'OnFocusChanged', value);
              }
          } catch (err) {
              console.error('[JS] Error sending focus message to Unity:', err);
          }
      }

      window._unityVisibilityCallback = function() {
          var hidden = document.hidden || document.webkitHidden;
          sendFocusToUnity(!hidden);
      };
      window._unityWindowBlurCallback  = function() { sendFocusToUnity(false); };
      window._unityWindowFocusCallback = function() { sendFocusToUnity(true); };

      document.removeEventListener('visibilitychange',       window._unityVisibilityCallback);
      document.removeEventListener('webkitvisibilitychange', window._unityVisibilityCallback);
      window.removeEventListener('blur',  window._unityWindowBlurCallback);
      window.removeEventListener('focus', window._unityWindowFocusCallback);

      document.addEventListener('visibilitychange',       window._unityVisibilityCallback);
      document.addEventListener('webkitvisibilitychange', window._unityVisibilityCallback);
      window.addEventListener('blur',  window._unityWindowBlurCallback);
      window.addEventListener('focus', window._unityWindowFocusCallback);
    }
});
