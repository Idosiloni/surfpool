using System.Collections.Generic;
using UnityEngine;

namespace SurfPool
{
    /// <summary>What happened to one surfer on their visit; written by <see cref="Surfer"/>, read when they leave.</summary>
    public class Visit
    {
        public string name;
        public Archetype type;
        public WaveLevel skill;
        public bool famous;
        public float arrivedAt, paidAt = -1f;
        public int barrels, tricks, wipeouts, spent, tip, photoGrade = -1;
        public bool droppedInOn, droppedIn, rescued, brokeBoard, dirtyPool;
        public string rodeWith;      // a legend they shared the water with
        public string angry;         // why they left angry (null if they didn't)
        public float happiness;
        public bool trash;

        public float Waited => (paidAt >= 0f ? paidAt : Time.time) - arrivedAt;
    }

    /// <summary>One written review: who, how many stars, what they said.</summary>
    public class Review
    {
        public string name, who, text;
        public int stars;
        public int interest; // how specific/funny it is (generic lines are 0)
    }

    /// <summary>
    /// Today's written reviews and fun facts, built from real events of each surfer's visit (<see cref="Visit"/>).
    /// The day report shows the best one, the worst one and a wild card, plus lines like "Longest wait: Kevin, 1:24".
    /// Only today's are kept (not saved): a new day starts a new page.
    /// </summary>
    public static class ParkReviews
    {
        static readonly List<Review> today = new();
        static Visit longestWait, bestCustomer, mostWipeouts;
        static int barrelsToday;
        static readonly List<string> extraFacts = new();

        /// <summary>Sneak-ins today (<see cref="RuleBreakers"/>).</summary>
        public static int SneaksCaught, SneaksEscaped;

        public static IReadOnlyList<Review> Today => today;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void NewDay()
        {
            today.Clear();
            longestWait = bestCustomer = mostWipeouts = null;
            barrelsToday = 0;
            extraFacts.Clear();
            SneaksCaught = SneaksEscaped = 0;
        }

        /// <summary>A line for today's fun facts from somewhere else (a rush, an event...).</summary>
        public static void AddFact(string fact) => extraFacts.Add(fact);

        /// <summary>A surfer is done (happy or angry): write their review and update the facts.</summary>
        public static Review Record(Visit visit)
        {
            var review = Write(visit);
            today.Add(review);
            if (visit.paidAt >= 0f || visit.angry != null && visit.angry.Contains("line"))
                if (longestWait == null || visit.Waited > longestWait.Waited) longestWait = visit;
            if (visit.spent > 0 && (bestCustomer == null || visit.spent > bestCustomer.spent)) bestCustomer = visit;
            if (visit.wipeouts >= 2 && (mostWipeouts == null || visit.wipeouts > mostWipeouts.wipeouts)) mostWipeouts = visit;
            barrelsToday += visit.barrels;
            return review;
        }

        static string Pick(params string[] lines) => lines[Random.Range(0, lines.Length)];

        static Review Write(Visit v)
        {
            string skill = v.famous ? "surf legend" : v.skill switch
            {
                WaveLevel.Beginner => "beginner",
                WaveLevel.Intermediate => "intermediate",
                _ => "pro",
            };
            var r = new Review { name = v.name, who = $"{v.name}, {skill}" };

            if (v.angry != null)
            {
                r.stars = 1;
                r.interest = 2;
                r.text = v.angry.Contains("helped") ? Pick("I was splashing and yelling HELP. Nobody came.", "Swallowed half the pool. Zero help.")
                    : v.angry.Contains("expensive") ? Pick("Way too pricey for a pool!", "My wallet is crying.", "Ever heard of a discount?")
                    : v.angry.Contains("waves") ? Pick("Paid for waves, got a very expensive bath.", "I sat on my board for AGES. No waves.", "Flat. Flat flat flat.")
                    : v.angry.Contains("board") ? Pick("Waited forever for a rental board. Gave up.", "Board rental? More like board NEVER-tal.")
                    : Pick("Stood in line forever. Never even got wet.", "The line was longer than the pool.", "I aged ten years in that line.");
                return r;
            }

            float h = v.happiness;
            r.stars = h > 0.9f ? 5 : h > 0.75f ? 4 : h > 0.5f ? 3 : h > 0.3f ? 2 : 1;
            if (v.rescued)
            {
                r.interest = 3;
                r.stars = Mathf.Max(r.stars, 4);
                r.text = Pick("Got tired, panicked, got saved with a ring buoy. Heroes work here!", "They threw me a ring! Best staff ever.",
                    "Almost became a fish. 10/10 rescue.");
            }
            else if (v.brokeBoard)
            {
                r.interest = 3;
                r.text = r.stars >= 3 ? Pick("Snapped my board in half! Still a sick day.", "Waves so good they broke my board. Respect.")
                    : Pick("Snapped my board. Not my day.", "RIP my board. RIP my wallet.");
            }
            else if (v.famous)
            {
                r.interest = 3;
                r.text = Pick("Fine little wave pool. I'll tell my friends.", "Your pool's got juice!", "Respect. Strong waves.");
            }
            else if (v.rodeWith != null)
            {
                r.interest = 3;
                r.text = $"I surfed next to {v.rodeWith}!! Unreal!";
                r.stars = Mathf.Max(r.stars, 4);
            }
            else if (v.barrels > 0 && r.stars >= 3)
            {
                r.interest = 3;
                r.text = Pick("Got BARRELED. Never washing this rash guard again.", "Tube ride! Best day of my life.", "Got shacked. Ten stars. TEN.");
            }
            else if (v.photoGrade >= 2 && r.stars >= 3)
            {
                r.interest = 2;
                r.text = Pick("Bought a sick photo of myself. Framing it.", "The photographer got my good side. All sides are my good side.");
            }
            else if (v.droppedInOn)
            {
                r.interest = 2;
                r.text = r.stars >= 3 ? "Some kook dropped in on me. Waves were fun, though." : "Got dropped in on. Nobody teaches manners anymore.";
            }
            else if (v.droppedIn)
            {
                r.interest = 2;
                r.text = Pick("Accidentally stole somebody's wave. Oops! Super fun though.", "Who knew you can't share waves? Not me!");
            }
            else if (v.wipeouts >= 2)
            {
                r.interest = 2;
                r.text = r.stars >= 3 ? $"Wiped out {v.wipeouts} times. Swallowed half the pool. Loved it." : $"Wiped out {v.wipeouts} times. The water won.";
            }
            else if (v.tricks >= 3 && r.stars >= 3 && v.skill != WaveLevel.Beginner)
            {
                r.interest = 2;
                r.text = Pick($"Landed {v.tricks} tricks. These waves are a skatepark!", $"{v.tricks} tricks in one session. New record!",
                    "Ramps made of water. Love it.");
            }
            else if (v.skill == WaveLevel.Beginner && r.stars >= 4 && Random.value < 0.6f)
            {
                r.interest = 2;
                r.text = Pick("I STOOD UP! I'm basically a pro now.", "First time surfing. Already looking at boards online.",
                    "Stood up for like 3 whole seconds. Legend.", "The waves were nice to me. Thank you, waves.");
            }
            else if (v.dirtyPool)
            {
                r.interest = 2;
                r.text = Pick("Swam through a salad of leaves and cups. Clean your pool!", "Something slimy touched my leg. Twice.",
                    "Great waves, dirty water.");
                r.stars = Mathf.Min(r.stars, 3);
            }
            else if (v.trash)
            {
                r.interest = 1;
                r.text = Pick("Good waves, but somebody please pick up that trash.", "Stepped on a cup. Ouch.",
                    "Nice pool. Trash everywhere, though.");
                r.stars = Mathf.Min(r.stars, 3);
            }
            else if (v.Waited > 60f && v.paidAt >= 0f)
            {
                r.interest = 1;
                r.text = r.stars >= 3 ? "Long line, but worth it." : "Long line AND meh waves.";
            }
            else if (v.tip >= 10)
            {
                r.interest = 1;
                r.text = "Worth every dollar. I even tipped!";
            }
            else
            {
                r.text = r.stars >= 4 ? Pick("Perfect little waves!", "Stoked! Coming back tomorrow.", "Ten out of ten, would paddle again.", "Best pool on the island.")
                    : r.stars == 3 ? Pick("Pretty fun. Could be bigger.", "Decent session.", "Not bad! Bit crowded.")
                    : Pick("Meh. Waves felt off.", "Not my day.", "Had better sessions in my bathtub.");
            }
            return r;
        }

        /// <summary>Up to three reviews for the report: the best, the worst, and a wild card.</summary>
        public static List<Review> Highlights()
        {
            var picks = new List<Review>();
            if (today.Count == 0) return picks;
            Review best = null, worst = null;
            foreach (var r in today)
            {
                if (best == null || r.stars * 10 + r.interest > best.stars * 10 + best.interest) best = r;
                if (worst == null || r.stars * 10 - r.interest < worst.stars * 10 - worst.interest) worst = r;
            }
            picks.Add(best);
            if (worst != best && worst.stars < best.stars) picks.Add(worst);
            var rest = new List<Review>();
            foreach (var r in today)
                if (!picks.Contains(r) && r.interest > 0) rest.Add(r);
            if (rest.Count == 0)
                foreach (var r in today)
                    if (!picks.Contains(r)) rest.Add(r);
            if (rest.Count > 0) picks.Add(rest[Random.Range(0, rest.Count)]);
            return picks;
        }

        /// <summary>Short fun facts with names in them, for the report.</summary>
        public static List<string> Facts()
        {
            var facts = new List<string>();
            if (bestCustomer != null) facts.Add($"Best customer: {bestCustomer.name}, {MoneyText.Format(bestCustomer.spent)}");
            if (longestWait != null && longestWait.Waited >= 20f)
            {
                int s = Mathf.RoundToInt(longestWait.Waited);
                facts.Add($"Longest wait: {longestWait.name}, {s / 60}:{s % 60:00}");
            }
            if (mostWipeouts != null) facts.Add($"Most wipeouts: {mostWipeouts.name} ({mostWipeouts.wipeouts})");
            if (barrelsToday > 0) facts.Add($"Barrels today: {barrelsToday}");
            facts.AddRange(extraFacts);
            if (SneaksCaught + SneaksEscaped > 0)
                facts.Add($"Sneak-ins: {SneaksCaught} caught" + (SneaksEscaped > 0 ? $", {SneaksEscaped} got away" : ""));
            return facts;
        }
    }
}
