using UnityEngine;

namespace SurfPool
{
    /// <summary>Who a surfer is (see docs/research/fun-surfers.md): changes what they say and how they celebrate.</summary>
    public enum Archetype { Grom, Kook, SoulCruiser, Charger, Influencer, Local, SurfDad }

    /// <summary>The moments a surfer says something about.</summary>
    public enum Moment { Catch, GoodEnd, OkEnd, BadEnd, Wipeout, Hoot, DropInVictim, DropInSorry }

    /// <summary>
    /// Surf types and their lines. Beginners are mostly kooks, groms and surf dads; intermediates cruise, film
    /// themselves or are groms getting good; pros are chargers and locals. Lines never repeat twice in a row.
    /// </summary>
    public static class SurferPersonalities
    {
        public static Archetype Roll(WaveLevel skill)
        {
            float r = Random.value;
            return skill switch
            {
                WaveLevel.Beginner => r < 0.45f ? Archetype.Kook : r < 0.75f ? Archetype.Grom : Archetype.SurfDad,
                WaveLevel.Intermediate => r < 0.3f ? Archetype.SoulCruiser : r < 0.55f ? Archetype.Influencer
                    : r < 0.75f ? Archetype.Grom : r < 0.9f ? Archetype.Local : Archetype.SurfDad,
                _ => r < 0.4f ? Archetype.Charger : r < 0.75f ? Archetype.Local : Archetype.Influencer,
            };
        }

        /// <summary>How each type celebrates a good ride.</summary>
        public static CharacterRig.Gesture Celebration(Archetype type) => type switch
        {
            Archetype.Charger => CharacterRig.Gesture.Claim,
            Archetype.SoulCruiser => CharacterRig.Gesture.SoulArch,
            Archetype.Local => CharacterRig.Gesture.Shaka,
            Archetype.Influencer => CharacterRig.Gesture.Shaka,
            Archetype.Grom => CharacterRig.Gesture.FistPump,
            Archetype.SurfDad => CharacterRig.Gesture.FistPump,
            _ => CharacterRig.Gesture.Claim, // kooks claim everything
        };

        static readonly string[][] Catch =
        {
            /* Grom */        new[] { "YEEEW!", "Mine mine mine!", "Watch this!!", "Send it!", "Let's gooo!" },
            /* Kook */        new[] { "Oh no oh no oh no", "Is this one mine?", "Here goes nothing!", "Paddle paddle paddle!", "Am I standing?!" },
            /* SoulCruiser */ new[] { "Ahh, there she is.", "Just flowing.", "Easy does it.", "Feel that glide.", "Pure stoke." },
            /* Charger */     new[] { "Too easy.", "Pitch it!", "Bring it on!", "Getting deep.", "Go time." },
            /* Influencer */  new[] { "Recording!", "This one's for the feed!", "Hi guys, watch!", "Content!", "Like and subscribe!" },
            /* Local */       new[] { "My wave.", "Been waiting all day.", "Like old times.", "Locals only!", "Classic." },
            /* SurfDad */     new[] { "Still got it!", "Kids, watch your dad!", "My back says no...", "Like it's 1995!", "Okay, okay, okay!" },
        };

        static readonly string[][] GoodEnd =
        {
            new[] { "BEST DAY EVER!", "Did you see that?!", "Again again again!", "I'm gonna be pro!" },
            new[] { "I STOOD UP!", "I'm a surfer now!", "Mom, I surfed!", "Totally on purpose!" },
            new[] { "Stoked.", "That was a dream.", "Pure flow, man.", "Life is good." },
            new[] { "Spat out clean.", "Easy.", "Ten points.", "Give me a bigger one." },
            new[] { "That's going viral!", "Got the shot!", "Ten thousand likes, easy!", "Did my hair survive?" },
            new[] { "Not bad for a pool.", "Best wave park around.", "Tell no one about this place.", "Respect." },
            new[] { "Still got it!", "Kids, did you film that?", "Where's my coffee?", "That's going on the fridge." },
        };

        static readonly string[][] Wipeout =
        {
            new[] { "AGAIN!", "That didn't count!", "Totally meant that!" },
            new[] { "WHERE IS MY BOARD", "Water up my nose!", "I'm okay! I'm okay!" },
            new[] { "Washing machine...", "Humbled.", "The ocean said no." },
            new[] { "Got pitched.", "Worth it.", "Next one." },
            new[] { "Delete that!", "My phone!!", "Was that recording?" },
            new[] { "Haven't done that in years.", "Shhh.", "Rusty today." },
            new[] { "My back!!", "Nobody saw that.", "I'm fine. FINE." },
        };

        static readonly string[] Hoot = { "Yeeew!", "Go go go!", "Hoooo!", "Get it!", "Shoot it!", "Paddle!" };
        static readonly string[] OkEnd = { "Pretty fun!", "Decent one.", "I'll take it.", "Not bad!" };
        static readonly string[] BadEnd = { "Meh. Wrong waves for me.", "Not my day.", "Hmm, could be better.", "Too mushy." };
        static readonly string[] Victim = { "HEY! My wave!", "Dude!! Drop-in!", "Seriously?!", "Watch out!" };
        static readonly string[] Sorry = { "Sorry!!", "Oops, my bad!", "I didn't see you!", "Is that not allowed?" };

        static int last = -1;

        public static string Line(Archetype type, Moment moment)
        {
            var pool = moment switch
            {
                Moment.Catch => Catch[(int)type],
                Moment.GoodEnd => GoodEnd[(int)type],
                Moment.Wipeout => Wipeout[(int)type],
                Moment.Hoot => Hoot,
                Moment.OkEnd => OkEnd,
                Moment.BadEnd => BadEnd,
                Moment.DropInVictim => Victim,
                _ => Sorry,
            };
            int i = Random.Range(0, pool.Length);
            if (i == last && pool.Length > 1) i = (i + 1) % pool.Length;
            last = i;
            return pool[i];
        }
    }
}
