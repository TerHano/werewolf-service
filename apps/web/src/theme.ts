import {
  createSystem,
  defaultConfig,
  defineTextStyles,
  defineTokens,
} from "@chakra-ui/react";

const textStyles = defineTextStyles({
  accent: {
    description: "Style used to display custom font",
    value: {
      fontFamily: "Poor Story",
    },
  },
});

const tokens = defineTokens({
  fonts: {
    body: {
      value: "'Poppins'",
    },
    heading: {
      value: `'Poppins'`,
    },
  },
});

export const system = createSystem(defaultConfig, {
  globalCss: {
    body: {
      //   bg: "bg.subtle",
    },
  },
  theme: {
    textStyles: textStyles,
    breakpoints: {
      xs: "22em",
    },
    tokens: tokens,
    // recipes: {
    //   Card: {
    //     base: { backgroundColor: "red" },
    //   },
    // },
  },
});
