mergeInto(LibraryManager.library, {
  IsMobileBrowser: function () {
    if (
      navigator.userAgentData &&
      typeof navigator.userAgentData.mobile === "boolean"
    ) {
      return navigator.userAgentData.mobile ? 1 : 0;
    }

    var userAgent = navigator.userAgent || navigator.vendor || "";
    var mobileUserAgent =
      /Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(
        userAgent
      );

    // Modern iPadOS can identify itself as macOS.
    var iPadDesktopMode =
      navigator.platform === "MacIntel" && navigator.maxTouchPoints > 1;

    return mobileUserAgent || iPadDesktopMode ? 1 : 0;
  },
});
