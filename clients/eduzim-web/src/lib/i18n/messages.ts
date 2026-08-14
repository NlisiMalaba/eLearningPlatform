export const en = {
  "app.name": "EduZim",
  "app.home.title": "Your learning home",
  "app.home.subtitle": "Continue your modules when you are ready.",
  "auth.login.title": "Sign in",
  "auth.login.subtitle": "Sign in to continue learning.",
  "auth.register.title": "Create an account",
  "auth.register.subtitle": "Join EduZim to start learning.",
  "auth.verify.title": "Verify your email",
  "auth.verify.subtitle": "Check your inbox to confirm your account.",
  "auth.sso.title": "School sign-in",
  "auth.sso.subtitle": "Continue with your school account.",
  "offline.banner":
    "You are offline. Progress will sync when connectivity is restored.",
  "offline.page.title": "You are offline",
  "offline.page.body":
    "Downloaded modules remain available. Your progress will upload when you are back online.",
} as const;

export const sn = {
  "app.name": "EduZim",
  "app.home.title": "Imba yako yekudzidza",
  "app.home.subtitle": "Enderera mberi nemamodule ako kana wagadzirira.",
  "auth.login.title": "Pinda",
  "auth.login.subtitle": "Pinda kuti uenderere mberi nekudzidza.",
  "auth.register.title": "Gara akaundi",
  "auth.register.subtitle": "Joinha EduZim kuti utange kudzidza.",
  "auth.verify.title": "Simbisa email yako",
  "auth.verify.subtitle": "Tarisa inbox yako kuti usimbise akaundi yako.",
  "auth.sso.title": "Kupinda kwechikoro",
  "auth.sso.subtitle": "Enderera mberi neakaundi yechikoro chako.",
  "offline.banner":
    "Hauna internet. Kufambira mberi kuchabatanidzwa kana internet yadzoka.",
  "offline.page.title": "Hauna internet",
  "offline.page.body":
    "Mamodule akatorwa anoramba achiwana. Kufambira mberi kuchakwidzwa kana wadzoka online.",
} as const;

export type MessageKey = keyof typeof en;
export type Locale = "en" | "sn";

export const dictionaries: Record<Locale, Record<MessageKey, string>> = {
  en,
  sn,
};
