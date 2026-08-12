import { Select, Portal, createListCollection } from "@chakra-ui/react";
import { useLocalStorage } from "@uidotdev/usehooks";
import { useEffect, useState } from "react";
import { useTranslation } from "react-i18next";

export const LanguageSelect = () => {
  const { i18n, t } = useTranslation();
  const [language, saveLanguage] = useLocalStorage("language", "en");
  const [value, setValue] = useState<string[]>([language]);

  useEffect(() => {
    const lang = value[0];
    if (lang) {
      i18n.changeLanguage(lang);
      saveLanguage(lang);
    }
  }, [i18n, saveLanguage, value]);

  return (
    <Select.Root
      size="xs"
      collection={languages}
      width="8rem"
      value={value}
      onValueChange={(e) => {
        if (!e.value[0]) return;
        setValue(e.value);
      }}
      variant="subtle"
    >
      <Select.HiddenSelect />
      <Select.Control>
        <Select.Trigger>
          <Select.ValueText placeholder={t("languageSelect.placeholder")} />
        </Select.Trigger>
        <Select.IndicatorGroup>
          <Select.Indicator />
        </Select.IndicatorGroup>
      </Select.Control>
      <Portal>
        <Select.Positioner>
          <Select.Content>
            {languages.items.map((language) => (
              <Select.Item item={language} key={language.value}>
                {language.label}
                <Select.ItemIndicator />
              </Select.Item>
            ))}
          </Select.Content>
        </Select.Positioner>
      </Portal>
    </Select.Root>
  );
};

const languages = createListCollection({
  items: [
    { value: "en", label: "English" },
    { value: "es", label: "Spanish" },
    { value: "zh", label: "Chinese" },
  ],
});
