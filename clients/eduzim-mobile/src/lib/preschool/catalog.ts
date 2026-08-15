import type { PreschoolLanguage } from "@/lib/preschool/languages";

export const FOUNDATIONAL_CATEGORIES = [
  "alphabet",
  "numbers",
  "shapes",
  "colours",
  "animals",
  "body",
  "vernacular",
] as const;

export type FoundationalCategory = (typeof FOUNDATIONAL_CATEGORIES)[number];

export type PreschoolItem = {
  id: string;
  category: FoundationalCategory;
  glyph: string;
  speak: Record<PreschoolLanguage, string>;
};

export function preschoolCatalog(): PreschoolItem[] {
  return [
    ...letters(),
    ...numbers(),
    ...named("shapes", [
      { id: "circle", glyph: "●", en: "circle", sn: "denderedzwa", nd: "indilinga" },
      { id: "square", glyph: "■", en: "square", sn: "sikweya", nd: "isikwele" },
      { id: "triangle", glyph: "▲", en: "triangle", sn: "gonyo nhatu", nd: "unxantathu" },
      { id: "star", glyph: "★", en: "star", sn: "nyeredzi", nd: "inkanyezi" },
      { id: "heart", glyph: "♥", en: "heart", sn: "mwoyo", nd: "inhliziyo" },
    ]),
    ...named("colours", [
      { id: "red", glyph: "🔴", en: "red", sn: "tsvuku", nd: "okubomvu" },
      { id: "green", glyph: "🟢", en: "green", sn: "girini", nd: "okuluhlaza" },
      { id: "blue", glyph: "🔵", en: "blue", sn: "bhuruu", nd: "okuluhlaza okwesibhakabhaka" },
      { id: "yellow", glyph: "🟡", en: "yellow", sn: "yero", nd: "okuphuzi" },
      { id: "orange", glyph: "🟠", en: "orange", sn: "orenji", nd: "i-orenji" },
    ]),
    ...named("animals", [
      { id: "lion", glyph: "🦁", en: "lion", sn: "shumba", nd: "ibhubesi" },
      { id: "elephant", glyph: "🐘", en: "elephant", sn: "nzou", nd: "indlovu" },
      { id: "cow", glyph: "🐄", en: "cow", sn: "mombe", nd: "inkomo" },
      { id: "goat", glyph: "🐐", en: "goat", sn: "mbudzi", nd: "imbuzi" },
      { id: "hen", glyph: "🐔", en: "hen", sn: "huku", nd: "inkukhu" },
      { id: "dog", glyph: "🐶", en: "dog", sn: "imbwa", nd: "inja" },
    ]),
    ...named("body", [
      { id: "head", glyph: "🙂", en: "head", sn: "musoro", nd: "ikhanda" },
      { id: "eyes", glyph: "👀", en: "eyes", sn: "maziso", nd: "amehlo" },
      { id: "hands", glyph: "🙌", en: "hands", sn: "maoko", nd: "izandla" },
      { id: "feet", glyph: "👣", en: "feet", sn: "tsoka", nd: "izinyawo" },
    ]),
    ...named("vernacular", [
      { id: "hello", glyph: "👋", en: "hello", sn: "mhoro", nd: "sawubona" },
      { id: "thank-you", glyph: "🙏", en: "thank you", sn: "waita zvakanaka", nd: "ngiyabonga" },
      { id: "water", glyph: "💧", en: "water", sn: "mvura", nd: "amanzi" },
      { id: "food", glyph: "🍲", en: "food", sn: "chikafu", nd: "ukudla" },
      { id: "mother", glyph: "👩", en: "mother", sn: "amai", nd: "umama" },
      { id: "father", glyph: "👨", en: "father", sn: "baba", nd: "ubaba" },
    ]),
  ];
}

export function itemsForCategory(category: FoundationalCategory): PreschoolItem[] {
  return preschoolCatalog().filter((item) => item.category === category);
}

function letters(): PreschoolItem[] {
  return Array.from({ length: 26 }, (_, index) => {
    const glyph = String.fromCharCode(65 + index);
    return {
      id: `letter-${glyph}`,
      category: "alphabet" as const,
      glyph,
      speak: { en: glyph, sn: glyph, nd: glyph },
    };
  });
}

function numbers(): PreschoolItem[] {
  return Array.from({ length: 100 }, (_, index) => {
    const value = index + 1;
    const spoken = String(value);
    return {
      id: `number-${value}`,
      category: "numbers" as const,
      glyph: spoken,
      speak: { en: spoken, sn: spoken, nd: spoken },
    };
  });
}

function named(
  category: FoundationalCategory,
  rows: { id: string; glyph: string; en: string; sn: string; nd: string }[],
): PreschoolItem[] {
  return rows.map((row) => ({
    id: `${category}-${row.id}`,
    category,
    glyph: row.glyph,
    speak: { en: row.en, sn: row.sn, nd: row.nd },
  }));
}
