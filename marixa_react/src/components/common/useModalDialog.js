import { useEffect, useRef, useState } from "react";

export default function useModalDialog() {
    const ref = useRef(null);
    // Capture before React mounts an autoFocus input inside the dialog.
    const [trigger] = useState(() => document.activeElement);
    useEffect(() => {
        const dialog = ref.current;
        dialog.showModal();
        return () => {
            dialog.close();
            if (trigger instanceof HTMLElement && trigger.isConnected) trigger.focus();
        };
    }, [trigger]);
    return ref;
}
