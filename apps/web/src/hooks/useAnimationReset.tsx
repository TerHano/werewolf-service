import { useState, useCallback } from "react";

export const useAnimationReset = () => {
  const [animation, setAnimation] = useState<string | undefined>("");

  const resetAnimation = useCallback(() => {
    setAnimation("none");
    setTimeout(() => {
      setAnimation("");
    }, 50);
  }, []);

  return { animation, resetAnimation };
};
