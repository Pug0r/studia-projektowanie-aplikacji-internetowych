/**
 * ScentMarket — Infinite Scroll helper
 * Uses IntersectionObserver to watch a sentinel element.
 * When it becomes visible, calls a .NET method via DotNetObjectReference.
 */
window.infiniteScroll = (() => {
    const observers = {};

    return {
        /**
         * Start observing an element. If already observed, the old observer
         * is disconnected first so there is never a duplicate.
         * Returns true if the element was found, false otherwise.
         */
        observe(elementId, dotNetRef, methodName) {
            // Clean up any existing observer for this element
            if (observers[elementId]) {
                observers[elementId].disconnect();
                delete observers[elementId];
            }

            const el = document.getElementById(elementId);
            if (!el) return false;

            const observer = new IntersectionObserver((entries) => {
                if (entries[0].isIntersecting) {
                    dotNetRef.invokeMethodAsync(methodName);
                }
            }, { rootMargin: '200px', threshold: 0 });

            observer.observe(el);
            observers[elementId] = observer;
            return true;
        },

        /** Stop observing an element and clean up. */
        unobserve(elementId) {
            if (observers[elementId]) {
                observers[elementId].disconnect();
                delete observers[elementId];
            }
        }
    };
})();
