// SwiftGrab Content Script – adds "Download with SwiftGrab" button to video pages

(function () {
  "use strict";

  function injectButton(linkEl) {
    if (linkEl.dataset.swiftgrabInjected) return;
    linkEl.dataset.swiftgrabInjected = "1";

    const btn = document.createElement("button");
    btn.textContent = "⚡ SwiftGrab";
    btn.title = "Download with SwiftGrab";
    btn.style.cssText =
      "margin-left:8px;padding:4px 10px;background:#1565C0;color:#fff;" +
      "border:none;border-radius:4px;cursor:pointer;font-size:12px;";

    btn.addEventListener("click", (e) => {
      e.preventDefault();
      e.stopPropagation();
      chrome.runtime.sendMessage({ type: "SWIFTGRAB_DOWNLOAD", url: linkEl.href });
    });

    linkEl.parentNode?.insertBefore(btn, linkEl.nextSibling);
  }

  // Inject buttons next to all download links on the page
  function scanLinks() {
    document.querySelectorAll('a[download], a[href$=".mp4"], a[href$=".mkv"], a[href$=".zip"]').forEach(injectButton);
  }

  scanLinks();
  const observer = new MutationObserver(scanLinks);
  observer.observe(document.body, { childList: true, subtree: true });
})();
