using UnityEngine;

namespace SurfPool
{
    /// <summary>What a person wears: the outfit decides which body bands get the top / bottom colors or skin.</summary>
    public enum Outfit { Tee, Shirtless, Tank, RashGuard, Springsuit, Fullsuit, Bikini, OnePiece }

    /// <summary>Prints laid on fabric (textures in Assets/_Game/Textures/Patterns, same order).</summary>
    public enum Pattern { None, Stripes, Hibiscus, Checker, Palms, Dots }

    /// <summary>Faces (see CharacterRig.Face): picked by what the person is doing, or set for a moment.</summary>
    public enum Face { Happy, Stoked, Focused, Scared, Grumpy, Sleepy }

    /// <summary>
    /// A person's whole look ("Chunky Beach Buddies", docs/research/character-look.md): body size, skin, hair,
    /// outfit + colors + prints, hat, glasses, facial hair and zinc nose. <see cref="CharacterRig.Wear"/> applies it.
    /// </summary>
    [System.Serializable]
    public class CharacterLook
    {
        public Color skin = new(0.88f, 0.68f, 0.52f);
        public Color hair = new(0.3f, 0.2f, 0.12f);
        [Tooltip("Index into the rig's hair styles; -1 = bald.")]
        public int hairStyle;
        public Outfit outfit = Outfit.Tee;
        public Color top = Color.white;
        [Tooltip("Second color: wetsuit chest panels.")]
        public Color accent = new(1f, 0.42f, 0.36f);
        public Color bottom = new(0.14f, 0.23f, 0.4f);
        public Pattern topPattern, bottomPattern;
        [Tooltip("-1 = no hat; else index into the rig's hats (cap, bucket, visor, headband).")]
        public int hat = -1;
        public Color hatColor = new(1f, 0.79f, 0.24f);
        [Tooltip("-1 = none; else sunnies, shield, round.")]
        public int glasses = -1;
        public Color lens = new(0.08f, 0.1f, 0.14f);
        public bool beard, mustache;
        [Tooltip("Zinc on the nose (alpha 0 = none).")]
        public Color zinc = Color.clear;
        [Tooltip("1 = adult. Groms are about 0.75.")]
        public float height = 1f;
        [Tooltip("Torso and hips width (1 = standard, 1.2 = round).")]
        public float width = 1f;
        [Tooltip("Extra head size on top of the house style (groms 1.15).")]
        public float head = 1f;
        public Face mood = Face.Happy;
        [Tooltip("Voice pitch for babble (1 = adult, 1.25 = higher voice, 1.45 = kid, 0.8 = deep).")]
        public float voice = 1f;
    }

    /// <summary>Builds looks: palettes, a signature look per surfer type, guests, staff and the famous surfers.</summary>
    public static class CharacterLooks
    {
        // Rig order: Short, Long, Bun, Curly, Buzz, Mohawk, Dreads, Ponytail, Mullet, Mop, Spiky.
        public const int Short = 0, Long = 1, Bun = 2, Curly = 3, Buzz = 4, Mohawk = 5, Dreads = 6, Ponytail = 7, Mullet = 8, Mop = 9, Spiky = 10;
        public const int Cap = 0, Bucket = 1, Visor = 2, Headband = 3;
        public const int Sunnies = 0, Shield = 1, Round = 2;

        static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;

        public static readonly Color[] Skins =
            { Hex("#FDE0C8"), Hex("#F4C79B"), Hex("#E0A672"), Hex("#C98550"), Hex("#A5673A"), Hex("#7B4A2A"), Hex("#573420"), Hex("#3D2416") };
        public static readonly Color[] Hairs =
            { Hex("#1E1A1A"), Hex("#4A2E1D"), Hex("#7A4A28"), Hex("#C9A15A"), Hex("#F0DC9A"), Hex("#B8502A") };
        public static readonly Color Gray = Hex("#B5B5B0"), White = Hex("#F2F2EE"), DyeTeal = Hex("#2FC4B2"), DyePink = Hex("#FF6FA5");
        public static readonly Color Coral = Hex("#FF6B5B"), Sun = Hex("#FFC93C"), Teal = Hex("#17B9B0"), Sky = Hex("#4FA8E8"),
            Navy = Hex("#233A66"), Lime = Hex("#8CD04F"), HotPink = Hex("#FF4F9A"), Orange = Hex("#FF9A2E"), Cloth = Hex("#F6F4EE"),
            Black = Hex("#2A2A30"), Denim = Hex("#6F8EA8"), Sand = Hex("#E8D5A8"), Purple = Hex("#8A5BD6");
        static readonly Color[] Fabrics = { Coral, Sun, Teal, Sky, Navy, Lime, HotPink, Orange, Cloth, Black, Denim, Sand, Purple };
        static readonly Color[] Faded = { Denim, Sand, Hex("#9BB7A0"), Hex("#C98A6B"), Hex("#7F9BB3"), Hex("#B3A07F") };

        static T Pick<T>(params T[] options) => options[Random.Range(0, options.Length)];
        static Color Skin() => Pick(Skins);
        static Color NaturalHair() => Random.value < 0.06f ? Gray : Pick(Hairs);

        /// <summary>Top and bottom must differ in brightness, so a body reads even from far away.</summary>
        static Color Contrast(Color against)
        {
            for (int i = 0; i < 8; i++)
            {
                var c = Pick(Fabrics);
                if (Mathf.Abs(c.grayscale - against.grayscale) > 0.22f) return c;
            }
            return against.grayscale > 0.5f ? Navy : Cloth;
        }

        /// <summary>A random beach-goer (guests, shoppers, spectators): tees, tanks, bikinis, aloha prints.</summary>
        public static CharacterLook Beachgoer()
        {
            var look = new CharacterLook { skin = Skin(), hair = NaturalHair(), top = Pick(Fabrics) };
            bool fem = UnityEngine.Random.value < 0.5f;
            look.hairStyle = fem ? Pick(Long, Bun, Ponytail, Curly, Long, Dreads) : Pick(Short, Buzz, Curly, Mullet, Short, Dreads, -1);
            look.outfit = fem ? Pick(Outfit.Tee, Outfit.Tank, Outfit.Bikini, Outfit.OnePiece) : Pick(Outfit.Tee, Outfit.Tank, Outfit.Shirtless, Outfit.Tee);
            look.bottom = Contrast(look.top);
            if (UnityEngine.Random.value < 0.25f) look.topPattern = Pick(Pattern.Hibiscus, Pattern.Stripes, Pattern.Palms, Pattern.Dots);
            if (UnityEngine.Random.value < 0.3f) look.bottomPattern = Pick(Pattern.Hibiscus, Pattern.Stripes, Pattern.Checker);
            if (UnityEngine.Random.value < 0.3f) look.hat = Pick(Cap, Bucket, Visor);
            look.hatColor = Pick(Fabrics);
            if (UnityEngine.Random.value < 0.35f) look.glasses = Pick(Sunnies, Round, Sunnies);
            if (!fem && UnityEngine.Random.value < 0.15f) look.beard = true;
            look.voice = fem ? UnityEngine.Random.Range(1.18f, 1.32f) : UnityEngine.Random.Range(0.85f, 1.0f);
            look.width = UnityEngine.Random.value < 0.2f ? 1.15f : UnityEngine.Random.Range(0.95f, 1.05f);
            look.height = UnityEngine.Random.Range(0.95f, 1.04f);
            return look;
        }

        /// <summary>A surfer of this type: a signature (always) plus random extras, so two kooks still differ.</summary>
        public static CharacterLook For(Archetype type)
        {
            var look = Beachgoer();
            look.topPattern = look.bottomPattern = Pattern.None;
            look.glasses = -1;
            look.hat = -1;
            look.beard = look.mustache = false;
            look.mood = Face.Happy;
            bool fem = UnityEngine.Random.value < 0.45f;
            look.voice = fem ? UnityEngine.Random.Range(1.18f, 1.32f) : UnityEngine.Random.Range(0.85f, 1.0f);
            switch (type)
            {
                case Archetype.Grom: // small, big head, neon rash vest + floral shorts, messy mop
                    look.height = 0.76f;
                    look.head = 1.15f;
                    look.width = 0.95f;
                    look.outfit = Outfit.RashGuard;
                    look.top = Pick(Lime, HotPink, Sun, Teal);
                    look.bottom = Pick(Navy, Black, Sky);
                    look.bottomPattern = Pick(Pattern.Hibiscus, Pattern.Checker, Pattern.None);
                    look.hairStyle = Pick(Mop, Mop, Spiky, Curly);
                    if (UnityEngine.Random.value < 0.15f) look.hair = Pick(DyeTeal, DyePink);
                    look.mood = Face.Stoked;
                    look.voice = UnityEngine.Random.Range(1.4f, 1.55f);
                    break;
                case Archetype.Kook: // clashing colors, a stiff new wetsuit or rash vest, visor, worried face
                    look.outfit = Pick(Outfit.Fullsuit, Outfit.RashGuard, Outfit.Springsuit);
                    look.top = Pick(Lime, HotPink, Orange);
                    look.accent = Pick(Navy, Purple, Sky);
                    look.bottom = Pick(Purple, Sky, HotPink);
                    if (UnityEngine.Random.value < 0.5f) { look.hat = Visor; look.hatColor = Pick(Lime, HotPink, Cloth); }
                    if (UnityEngine.Random.value < 0.3f) look.zinc = Cloth;
                    look.mood = Face.Scared;
                    break;
                case Archetype.SoulCruiser: // 60s longboarder: long striped or aloha jams, bare chest or striped tee, bucket hat
                    look.outfit = fem ? Pick(Outfit.OnePiece, Outfit.Tee) : Pick(Outfit.Shirtless, Outfit.Tee);
                    look.top = Pick(Faded);
                    look.topPattern = look.outfit == Outfit.Tee ? Pattern.Stripes : Pattern.None;
                    look.bottom = Pick(Teal, Faded[2], Coral);
                    look.bottomPattern = Pick(Pattern.Stripes, Pattern.Hibiscus);
                    look.hair = Pick(Hex("#F0DC9A"), Hex("#C9A15A"));
                    look.hairStyle = fem ? Pick(Long, Bun) : Pick(Long, Bun, Curly);
                    if (UnityEngine.Random.value < 0.4f) { look.hat = Bucket; look.hatColor = Pick(Sand, Faded[2]); }
                    look.width = 0.95f;
                    break;
                case Archetype.Charger: // athletic: black springsuit or rash vest with one neon, headband, focused
                    look.outfit = Pick(Outfit.Springsuit, Outfit.RashGuard, Outfit.Fullsuit);
                    look.top = Black;
                    look.accent = Pick(Orange, Lime, Sun);
                    look.bottom = Black;
                    look.hat = UnityEngine.Random.value < 0.5f ? Headband : -1;
                    look.hatColor = look.accent;
                    look.hairStyle = Pick(Short, Buzz, Mohawk, Ponytail);
                    look.width = 1.05f;
                    look.height = 1.03f;
                    look.mood = Face.Focused;
                    break;
                case Archetype.Influencer: // matching set, sunglasses, man bun or high pony
                    look.outfit = fem ? Pick(Outfit.Bikini, Outfit.Springsuit) : Pick(Outfit.Shirtless, Outfit.Tank);
                    look.top = Pick(Coral, HotPink, Teal, Cloth);
                    look.bottom = look.outfit == Outfit.Shirtless ? Pick(Coral, Teal) : look.top;
                    look.accent = Cloth;
                    look.glasses = Pick(Sunnies, Round);
                    look.lens = Pick(new Color(0.08f, 0.1f, 0.14f), new Color(0.95f, 0.45f, 0.6f), new Color(0.3f, 0.75f, 0.95f));
                    look.hairStyle = fem ? Ponytail : Bun;
                    if (UnityEngine.Random.value < 0.25f) look.hair = Pick(DyePink, Hex("#F0DC9A"));
                    look.width = 0.95f;
                    break;
                case Archetype.Local: // weathered: faded trucker cap, white zinc nose, stubble, mullet, faded checker trunks
                    look.outfit = Pick(Outfit.Shirtless, Outfit.Tank, Outfit.Fullsuit);
                    look.top = Pick(Faded);
                    look.accent = Black;
                    look.bottom = Pick(Faded);
                    look.bottomPattern = Pick(Pattern.Checker, Pattern.None);
                    look.hairStyle = Pick(Mullet, Dreads, Short);
                    look.hair = UnityEngine.Random.value < 0.4f ? Gray : look.hair;
                    look.hat = UnityEngine.Random.value < 0.55f ? Cap : -1;
                    look.hatColor = Pick(Faded);
                    look.zinc = UnityEngine.Random.value < 0.6f ? Cloth : Color.clear;
                    look.beard = UnityEngine.Random.value < 0.5f;
                    look.mustache = !look.beard && UnityEngine.Random.value < 0.3f;
                    look.width = 1.08f;
                    look.skin = Pick(Skins[3], Skins[4], Skins[5], Skins[6]);
                    look.mood = Face.Grumpy;
                    look.voice *= 0.88f;
                    break;
                case Archetype.SurfDad: // belly, aloha shirt or tank, sun hat or visor, sunscreen
                    look.outfit = Pick(Outfit.Tee, Outfit.Tank, Outfit.Tee);
                    look.top = Pick(Sky, Coral, Teal, Sun);
                    look.topPattern = look.outfit == Outfit.Tee ? Pattern.Hibiscus : Pattern.None;
                    look.bottom = Pick(Navy, Denim, Black);
                    look.hat = Pick(Visor, Bucket, Cap);
                    look.hatColor = Pick(Cloth, Sand, Navy);
                    look.glasses = UnityEngine.Random.value < 0.5f ? Sunnies : -1;
                    look.hairStyle = Pick(Short, Buzz, -1);
                    look.mustache = UnityEngine.Random.value < 0.35f;
                    look.zinc = UnityEngine.Random.value < 0.3f ? Cloth : Color.clear;
                    look.width = 1.22f;
                    look.height = 0.98f;
                    look.voice = UnityEngine.Random.Range(0.78f, 0.88f);
                    break;
            }
            return look;
        }

        /// <summary>A staff uniform: shirt color by job, khaki or navy shorts, a cap for some.</summary>
        public static CharacterLook Staff(Color shirt, Color shorts, Color skin, Color hair, int hairStyle)
        {
            return new CharacterLook
            {
                skin = skin, hair = hair, hairStyle = hairStyle, outfit = Outfit.Tee, top = shirt, bottom = shorts,
                hat = -1, glasses = -1, mood = Face.Happy,
            };
        }

        /// <summary>The old 4-color paint, as a look (a tee and shorts).</summary>
        public static CharacterLook Simple(Color skin, Color shirt, Color shorts, Color hair, int hairStyle) =>
            new() { skin = skin, top = shirt, bottom = shorts, hair = hair, hairStyle = hairStyle, outfit = Outfit.Tee };
    }
}
