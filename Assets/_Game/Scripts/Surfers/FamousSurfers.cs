using UnityEngine;

namespace SurfPool
{
    /// <summary>One of the surf legends (invented people) who visit famous parks.</summary>
    public class FamousSurfer
    {
        public string id, name, nickname;
        public Archetype type;
        public int level;
        public string signature;
        public string[] catchLines, endLines;
        public Color board;
        public System.Func<CharacterLook> look;

        public string FullName => $"{name.Split(' ')[0]} \"{nickname}\" {name.Substring(name.IndexOf(' ') + 1)}";
    }

    /// <summary>
    /// The legends roster. They are made-up surfers (no real people), each with a signature look, board, trick and
    /// lines. From park level 15 (with Pro waves) one of them drops by now and then (<see cref="FamousVisits"/>).
    /// </summary>
    public static class FamousSurfers
    {
        static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;

        public static readonly FamousSurfer[] Roster =
        {
            new()
            {
                id = "leilani", name = "Leilani Kealoha", nickname = "Lightning", type = Archetype.Charger, level = 15,
                signature = "LIGHTNING AIR!", board = Hex("#17B9B0"),
                catchLines = new[] { "Catch me if you can!", "Lightning never waits.", "Here comes the storm!" },
                endLines = new[] { "Electric!", "Your pool's got juice!", "I'll be back for more!" },
                look = () => new CharacterLook
                {
                    skin = CharacterLooks.Skins[3], hair = Hex("#1E1A1A"), hairStyle = CharacterLooks.Ponytail, outfit = Outfit.Springsuit,
                    top = Hex("#F6F4EE"), accent = Hex("#17B9B0"), bottom = Hex("#F6F4EE"), glasses = CharacterLooks.Shield,
                    lens = Hex("#4FA8E8"), mood = Face.Focused, voice = 1.24f, width = 0.97f, height = 1.02f,
                },
            },
            new()
            {
                id = "jett", name = "Jett Nakamura", nickname = "Kid Comet", type = Archetype.Grom, level = 15,
                signature = "COMET SPIN!", board = Hex("#8CD04F"),
                catchLines = new[] { "Comet incoming!!", "Watch THIS!", "Too easy, bro!" },
                endLines = new[] { "Did you film it?!", "Again! AGAIN!", "Best pool ever!!" },
                look = () => new CharacterLook
                {
                    skin = CharacterLooks.Skins[1], hair = Hex("#1E1A1A"), hairStyle = CharacterLooks.Spiky, outfit = Outfit.RashGuard,
                    top = Hex("#8CD04F"), bottom = Hex("#233A66"), bottomPattern = Pattern.Checker, hat = CharacterLooks.Cap,
                    hatColor = Hex("#FF4F9A"), mood = Face.Stoked, voice = 1.5f, height = 0.76f, head = 1.15f, width = 0.95f,
                },
            },
            new()
            {
                id = "june", name = "June Albright", nickname = "Mama Wave", type = Archetype.SoulCruiser, level = 16,
                signature = "HANG TEN!", board = Hex("#FFC93C"),
                catchLines = new[] { "Easy now, darling.", "Seventy years young!", "Let her glide." },
                endLines = new[] { "Lovely little wave.", "You run a fine pool, dear.", "That one was poetry." },
                look = () => new CharacterLook
                {
                    skin = CharacterLooks.Skins[1], hair = Hex("#F2F2EE"), hairStyle = CharacterLooks.Bun, outfit = Outfit.OnePiece,
                    top = Hex("#FF6B5B"), glasses = CharacterLooks.Round, lens = Hex("#2A2A30"), mood = Face.Happy, voice = 1.08f, width = 1.02f,
                    height = 0.96f,
                },
            },
            new()
            {
                id = "skip", name = "Skip Halloway", nickname = "Log Daddy", type = Archetype.SoulCruiser, level = 17,
                signature = "NOSE RIDE!", board = Hex("#F6F4EE"),
                catchLines = new[] { "Groovy.", "Walking the board, baby.", "Old school rules." },
                endLines = new[] { "Far out, man.", "Like '66 all over again.", "Peace and waves." },
                look = () => new CharacterLook
                {
                    skin = CharacterLooks.Skins[2], hair = Hex("#C9A15A"), hairStyle = CharacterLooks.Long, outfit = Outfit.Shirtless,
                    bottom = Hex("#FF6B5B"), bottomPattern = Pattern.Hibiscus, hat = CharacterLooks.Bucket, hatColor = Hex("#E8D5A8"),
                    mustache = true, mood = Face.Happy, voice = 0.86f,
                },
            },
            new()
            {
                id = "rio", name = "Rio Santos", nickname = "Rocket", type = Archetype.Charger, level = 18,
                signature = "ROCKET 540!", board = Hex("#FFC93C"),
                catchLines = new[] { "3, 2, 1... LIFTOFF!", "Sending it to the moon!", "Vamos!" },
                endLines = new[] { "Rocket landed!", "Obrigado, pool!", "Next one goes higher!" },
                look = () => new CharacterLook
                {
                    skin = CharacterLooks.Skins[4], hair = Hex("#F0DC9A"), hairStyle = CharacterLooks.Mohawk, outfit = Outfit.RashGuard,
                    top = Hex("#FFC93C"), bottom = Hex("#2A2A30"), mood = Face.Stoked, voice = 0.98f, width = 1.0f, height = 1.03f,
                },
            },
            new()
            {
                id = "bruno", name = "Bruno Kahale", nickname = "The Mountain", type = Archetype.Local, level = 19,
                signature = "MEGA BARREL!", board = Hex("#FF9A2E"),
                catchLines = new[] { "Bring the big one.", "Mountain's moving.", "Now THIS is a wave." },
                endLines = new[] { "Respect, boss.", "Strong pool.", "Bigger next time." },
                look = () => new CharacterLook
                {
                    skin = CharacterLooks.Skins[5], hair = Hex("#1E1A1A"), hairStyle = CharacterLooks.Dreads, outfit = Outfit.Fullsuit,
                    top = Hex("#2A2A30"), accent = Hex("#FF9A2E"), beard = true, mood = Face.Focused, voice = 0.76f, width = 1.24f,
                    height = 1.06f,
                },
            },
            new()
            {
                id = "ingrid", name = "Ingrid Lindqvist", nickname = "Ice", type = Archetype.Charger, level = 20,
                signature = "ICE PICK TUBE!", board = Hex("#4FA8E8"),
                catchLines = new[] { "Warm water. Cute.", "Ice in the veins.", "Stay frosty." },
                endLines = new[] { "Acceptable.", "Clean barrel. Good.", "Ten out of ten. Maybe." },
                look = () => new CharacterLook
                {
                    skin = CharacterLooks.Skins[0], hair = Hex("#F2F2EE"), hairStyle = CharacterLooks.Ponytail, outfit = Outfit.Fullsuit,
                    top = Hex("#233A66"), accent = Hex("#4FA8E8"), glasses = -1, zinc = Hex("#4FA8E8"), mood = Face.Focused,
                    voice = 1.16f, height = 1.05f,
                },
            },
        };

        public static FamousSurfer Find(string id) => System.Array.Find(Roster, f => f.id == id);
    }
}
