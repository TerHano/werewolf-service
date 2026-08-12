import i18next from "i18next";
import enTranslation from "@/locales/en.json";
import esTranslation from "@/locales/es.json";
import zhTranslation from "@/locales/zh.json";
import { initReactI18next } from "react-i18next";

i18next.use(initReactI18next).init({
  debug: false,
  lng: "en",
  fallbackLng: "en",
  resources: {
    en: { translation: enTranslation },
    es: { translation: esTranslation },
    zh: { translation: zhTranslation },
  },
});
