mergeInto(LibraryManager.library, {
    // Outbound: Unity -> iframe host, as { type, data } via window.parent.postMessage.
    SendPostMessage: function(messagePtr)
    {
      var message = UTF8ToString(messagePtr);
      if (typeof window !== "undefined" && window.parent && typeof window.parent.postMessage === "function") {
        window.parent.postMessage({
          type: message,
          data: { }
        }, "*");
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
    },

    // Self-contained resize bridge: the Unity page listens to its own viewport and pushes
    // "width,height" into Unity (OC.SwitchDisplay) — no dependency on the iframe host.
    // No debounce: SwitchDisplay's RotationCoroutine (StopCoroutine + WaitForSecondsRealtime)
    // already coalesces rapid calls.
    RegisterResizeListener: function (gameObjectNamePtr, methodNamePtr) {
      var gameObjectName = UTF8ToString(gameObjectNamePtr);
      var methodName = UTF8ToString(methodNamePtr);

      function sendDimensionsToUnity() {
        try {
          // visualViewport is the accurate visible area on iOS; fall back to innerWidth/Height.
          var vv = window.visualViewport;
          var w = Math.round(vv ? vv.width : window.innerWidth);
          var h = Math.round(vv ? vv.height : window.innerHeight);
          var dimensions = w + ',' + h;
          if (typeof SendMessage === 'function') {
            SendMessage(gameObjectName, methodName, dimensions);
          } else if (typeof unityInstance !== 'undefined' && unityInstance && unityInstance.SendMessage) {
            unityInstance.SendMessage(gameObjectName, methodName, dimensions);
          }
        } catch (err) {
          console.error('[JS] resize send failed:', err);
        }
      }

      if (window._unityResizeCallback) {
        window.removeEventListener('resize', window._unityResizeCallback);
        window.removeEventListener('orientationchange', window._unityResizeCallback);
        if (window.visualViewport) window.visualViewport.removeEventListener('resize', window._unityResizeCallback);
      }
      window._unityResizeCallback = sendDimensionsToUnity;
      window.addEventListener('resize', window._unityResizeCallback);
      window.addEventListener('orientationchange', window._unityResizeCallback);
      if (window.visualViewport) window.visualViewport.addEventListener('resize', window._unityResizeCallback);

      sendDimensionsToUnity();   // initial sync
    },

    // Inbound auth: host posts { type:"TokenReceived", data:{cookie,socketURL,nameSpace} } -> Unity.
    RegisterTokenListener: function (gameObjectNamePtr, methodNamePtr) {
      var gameObjectName = UTF8ToString(gameObjectNamePtr);
      var methodName = UTF8ToString(methodNamePtr);

      if (window._unityTokenCallback) {
        window.removeEventListener('message', window._unityTokenCallback);
      }
      window._unityTokenCallback = function (event) {
        if (!event.data || event.data.type !== 'TokenReceived') return;
        var json = JSON.stringify(event.data.data);
        if (typeof SendMessage === 'function') {
          SendMessage(gameObjectName, methodName, json);
        } else if (typeof unityInstance !== 'undefined' && unityInstance && unityInstance.SendMessage) {
          unityInstance.SendMessage(gameObjectName, methodName, json);
        }
      };
      window.addEventListener('message', window._unityTokenCallback);
    }
});
