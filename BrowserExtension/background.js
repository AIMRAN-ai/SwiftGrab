// SwiftGrab Browser Extension – Background Service Worker
const NATIVE_HOST = "com.swiftgrab.host";

// ── Context menu ─────────────────────────────────────────────────────────────
chrome.runtime.onInstalled.addListener(() => {
  chrome.contextMenus.create({
    id: "swiftgrab-download",
    title: "Download with SwiftGrab",
    contexts: ["link", "video", "audio"],
  });
});

chrome.contextMenus.onClicked.addListener((info) => {
  const url = info.linkUrl || info.srcUrl || info.pageUrl;
  if (url) sendToNativeHost(url);
});

// ── Messages from content script ─────────────────────────────────────────────
chrome.runtime.onMessage.addListener((msg) => {
  if (msg.type === "SWIFTGRAB_DOWNLOAD" && msg.url) {
    sendToNativeHost(msg.url);
  }
});

// ── Native messaging ──────────────────────────────────────────────────────────
function sendToNativeHost(url) {
  chrome.runtime.sendNativeMessage(NATIVE_HOST, { action: "download", url }, (response) => {
    if (chrome.runtime.lastError) {
      console.error("SwiftGrab native host error:", chrome.runtime.lastError.message);
      // Fallback: open via custom protocol
      chrome.tabs.create({ url: `swiftgrab://download?url=${encodeURIComponent(url)}` });
    } else {
      console.log("SwiftGrab response:", response);
    }
  });
}
