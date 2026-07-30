import { useCallback, useMemo } from "react";

const avatarDirectory = "/src/assets/icons/avatars";

// Vite replaces this with a static module map at build time. Kept at module scope so the
// map is not rebuilt on every single avatar lookup.
const avatarModules = import.meta.glob(`/src/assets/icons/avatars/*`, {
  eager: true,
}) as Record<string, { default: string }>;

export const usePlayerAvatar = () => {
  const data = useMemo(
    () => [
      "farmer-1",
      "farmer-2",
      "farmer",
      "halloween",
      "man-1",
      "man-2",
      "man-3",
      "man-4",
      "man-5",
      "man",
      "oktoberfest",
      "pilgrim",
      //'king',
      "thanksgiving",
      "tyrolean-1",
      "tyrolean",
      "woman-1",
      "woman-2",
      "woman-3",
      "woman-4",
      "woman",
      "thanksgiving-man",
      // 'pirate',
      // 'police-dog',
      // 'princess',
      // 'punk-1',
      // 'punk',
      // 'robot',
      // 'siberian-husky',
      // 'spy',
      // 'squirrel',
      // 'student',
      // 'vampire',
      // 'viking',
    ],
    []
  );

  const getAvatarImageSrcForIndex = useCallback(
    (avatarIndex?: number) => {
      // Valid indices are 0..data.length-1. The old check used `> data.length`, so an
      // index of exactly data.length slipped through and read past the end of the array;
      // negative indices were not caught either. Anything out of range falls back to the
      // first avatar rather than throwing on `undefined.default`.
      const isInRange =
        avatarIndex !== undefined &&
        Number.isInteger(avatarIndex) &&
        avatarIndex >= 0 &&
        avatarIndex < data.length;
      const avatarName = data[isInRange ? avatarIndex : 0];
      return (
        avatarModules[`${avatarDirectory}/${avatarName}.png`]?.default ?? ""
      );
    },
    [data]
  );

  return { data, getAvatarImageSrcForIndex };
};
