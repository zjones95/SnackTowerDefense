// Browser clipboard bridge for WebGL. GUIUtility.systemCopyBuffer does not work
// in the browser, so the "Copy" button calls these JS functions instead.
mergeInto(LibraryManager.library, {
  TD_CopyToClipboard: function (textPtr) {
    var text = UTF8ToString(textPtr);
    try {
      if (navigator.clipboard && navigator.clipboard.writeText) {
        navigator.clipboard.writeText(text);
        return;
      }
    } catch (e) { /* fall through to the legacy path */ }

    // Fallback for non-secure contexts (e.g. plain http on a LAN) or older browsers.
    try {
      var ta = document.createElement('textarea');
      ta.value = text;
      ta.style.position = 'fixed';
      ta.style.top = '-1000px';
      ta.style.opacity = '0';
      document.body.appendChild(ta);
      ta.focus();
      ta.select();
      document.execCommand('copy');
      document.body.removeChild(ta);
    } catch (e) { }
  }
});
