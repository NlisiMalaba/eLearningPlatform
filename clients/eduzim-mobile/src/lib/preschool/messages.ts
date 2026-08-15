import type { PreschoolLanguage } from "@/lib/preschool/languages";

export type PreschoolMessageKey =
  | "title"
  | "subtitle"
  | "stars"
  | "language"
  | "back"
  | "school"
  | "openToddler"
  | "rest.title"
  | "rest.body"
  | "rest.continue"
  | "pause.title"
  | "pause.body"
  | "pause.resume"
  | "celebration"
  | "starAward"
  | "done"
  | "character"
  | "alphabet"
  | "numbers"
  | "shapes"
  | "colours"
  | "animals"
  | "body"
  | "vernacular";

const messages: Record<PreschoolLanguage, Record<PreschoolMessageKey, string>> = {
  en: {
    title: "Let's play and learn",
    subtitle: "Tap a picture. Zuzu will say the word.",
    stars: "Stars",
    language: "Language",
    back: "Back",
    school: "School modules",
    openToddler: "Toddler learning",
    "rest.title": "Time to rest",
    "rest.body": "You have been playing for 20 minutes. Stretch, then tap continue.",
    "rest.continue": "I am ready",
    "pause.title": "Are you still there?",
    "pause.body": "The game paused. Tap to play again.",
    "pause.resume": "Play again",
    celebration: "Well done!",
    starAward: "You earned a star",
    done: "Finished",
    character: "Zuzu the learning friend",
    alphabet: "Letters",
    numbers: "Numbers",
    shapes: "Shapes",
    colours: "Colours",
    animals: "Animals",
    body: "Body",
    vernacular: "Words",
  },
  sn: {
    title: "Ngatitambire tichidzidza",
    subtitle: "Tinya mufananidzo. Zuzu anotaura izwi.",
    stars: "Nyeredzi",
    language: "Mutauro",
    back: "Dzoka",
    school: "Zvidzidzo zvechikoro",
    openToddler: "Kudzidza kwevana vadiki",
    "rest.title": "Nguva yekuzorora",
    "rest.body": "Wanga uchitamba kwemaminitsi makumi maviri. Simuka, wobva watinya continue.",
    "rest.continue": "Ndakagadzirira",
    "pause.title": "Uchiri pano here?",
    "pause.body": "Mutambo wakamira. Tinya kuti utambe zvakare.",
    "pause.resume": "Tamba zvakare",
    celebration: "Zvakanaka!",
    starAward: "Wawana nyeredzi",
    done: "Ndapera",
    character: "Zuzu shamwari yekudzidza",
    alphabet: "Mavara",
    numbers: "Nhamba",
    shapes: "Maumbirwo",
    colours: "Mavara",
    animals: "Mhuka",
    body: "Muviri",
    vernacular: "Mazwi",
  },
  nd: {
    title: "Asidlale sifunde",
    subtitle: "Thepha isithombe. UZuzu uzosho igama.",
    stars: "Izinkanyezi",
    language: "Ulimi",
    back: "Buyela",
    school: "Izifundo zesikole",
    openToddler: "Ukufunda kwengane",
    "rest.title": "Isikhathi sokuphumula",
    "rest.body": "Udlale imizuzu engamashumi amabili. Nweba umzimba, bese uthinta qhubeka.",
    "rest.continue": "Ngilungile",
    "pause.title": "Usakhona yini?",
    "pause.body": "Umdlalo umile. Thepha ukuze udlale futhi.",
    "pause.resume": "Dlala futhi",
    celebration: "Kuhle kakhulu!",
    starAward: "Uzuze inkanyezi",
    done: "Ngiqedile",
    character: "UZuzu umngane wokufunda",
    alphabet: "Ohlamvu",
    numbers: "Izinombolo",
    shapes: "Ukuma",
    colours: "Imibala",
    animals: "Izilwane",
    body: "Umzimba",
    vernacular: "Amagama",
  },
};

export function preschoolT(key: PreschoolMessageKey, language: PreschoolLanguage): string {
  return messages[language][key];
}
