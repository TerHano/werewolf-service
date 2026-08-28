import { useCallback, useState } from "react";
import { readFlag, writeFlag } from "@/util/preference";

const DISMISSED_KEY = "werewolf.installPromptDismissed";

/**
 * Whether to offer this device the "add to home screen" explanation.
 *
 * Narrow on purpose. It asks for four things at once:
 *
 * - iOS, because the steps we show are Safari's and nobody else's.
 * - Safari specifically. Chrome, Firefox and Edge on iOS cannot add to the home screen at all,
 *   so telling their users to tap Share would send them looking for a menu item that is not
 *   there.
 * - Not already installed. `navigator.standalone` is the iOS signal for "launched from the
 *   home screen"; display-mode covers everyone else.
 * - Not already dismissed. Once said no, it stays no — this is a suggestion, not a demand.
 */
export const useIosInstall = () => {
  const [isDismissed, setDismissed] = useState(() =>
    readFlag(DISMISSED_KEY, false)
  );

  const dismiss = useCallback(() => {
    setDismissed(true);
    writeFlag(DISMISSED_KEY, true);
  }, []);

  if (typeof window === "undefined" || typeof navigator === "undefined") {
    return { canInstall: false, dismiss };
  }

  const agent = navigator.userAgent;
  const isIos = /iphone|ipad|ipod/i.test(agent);
  // Every iOS browser reports Safari; the others add their own token first.
  const isSafari = !/crios|fxios|edgios|opios/i.test(agent);
  const isInstalled =
    window.matchMedia("(display-mode: standalone)").matches ||
    (window.navigator as { standalone?: boolean }).standalone === true;

  return {
    canInstall: isIos && isSafari && !isInstalled && !isDismissed,
    dismiss,
  };
};
