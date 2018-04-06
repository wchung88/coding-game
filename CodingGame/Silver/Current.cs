using System;
using System.Collections.Generic;
using System.Linq;

class Player
{
    static void Main(string[] args)
    {
        string[] inputs;
        int myTeam = int.Parse(Console.ReadLine());
        int bushAndSpawnPointCount =
            int.Parse(Console
                .ReadLine()); // useful from wood1, represents the number of bushes and the number of places where neutral units can spawn

        List<Bush> bushes = new List<Bush>();

        for (int i = 0; i < bushAndSpawnPointCount; i++)
        {
            inputs = Console.ReadLine().Split(' ');
            bushes.Add(new Bush(inputs));
        }
        int itemCount = int.Parse(Console.ReadLine()); // useful from wood2

        List<Item> shop = new List<Item>();
        for (int i = 0; i < itemCount; i++)
        {
            inputs = Console.ReadLine().Split(' ');
            var item = new Item(inputs);
            shop.Add(item);
        }

        List<Hero> myHeros = new List<Hero>
        {
            new Hero("HULK", 400),
            new Hero("DEADPOOL", 600)
        };

        int roundNo = 1;
        Dictionary<string, Hero> enemyHeros = new Dictionary<string, Hero>();
        // game loop
        while (true)
        {
            int gold = int.Parse(Console.ReadLine());
            int enemyGold = int.Parse(Console.ReadLine());
            int roundType =
                int.Parse(Console.ReadLine()); // a positive value will show the number of heroes that await a command
            int entityCount = int.Parse(Console.ReadLine());

            List<Entity> entities = new List<Entity>();
            for (int i = 0; i < entityCount; i++)
            {
                inputs = Console.ReadLine().Split(' ');
                var e = new Entity(inputs);
                entities.Add(e);
            }

            Console.Error.WriteLine("RoundType" + roundNo);

            if (roundType < -1)
            {
                Console.WriteLine(myHeros[0].Name);
            }
            else if (roundType < 0)
            {
                Console.WriteLine(myHeros[1].Name);
            }
            else
            {

                foreach (var eHero in entities.Where(x => x.team != myTeam && x.heroType != "-"))
                {
                    if (enemyHeros.ContainsKey(eHero.heroType))
                    {
                        enemyHeros[eHero.heroType].Previous = enemyHeros[eHero.heroType].Current;
                        enemyHeros[eHero.heroType].Current = eHero;
                        var previous = enemyHeros[eHero.heroType].Previous;
                        if (previous.x == eHero.x && previous.y == eHero.y)
                        {
                            enemyHeros[eHero.heroType].NoOfTurnsSinceLastMove++;
                        }
                        else
                        {
                            enemyHeros[eHero.heroType].NoOfTurnsSinceLastMove = 0;
                        }
                    }
                    else
                    {
                        enemyHeros.Add(eHero.heroType, new Hero() { Current = eHero });
                    }
                }

                foreach (var hero in myHeros)
                {
                    var util = new Util();
                    util.UpdateWithRoundInfo(bushes, hero.Name, entities, myTeam, shop, gold, hero);

                    if (util.myCurrentHero != null)
                    {
                        Console.Error.WriteLine("Inside here?123");
                        HeroCommands(util, enemyHeros);
                    }

                    hero.Previous = util.myCurrentHero;
                }
            }

            roundNo++;
        }
    }

    private static void HeroCommands(Util util, Dictionary<string, Hero> enemyHeros)
    {
        var affordableItems = util.FindAffordableWeapon();
        var potionToBuy = util.GetPotionsIfNeeded();
        var myCurrentHero = util.myCurrentHero;
        var previousHero = util.myPreviousHero;
        var myHero = util.Hero;
        //  && !AreEnemyInRangeOfMyTower(entities, myTeam).Any()
        if (enemyHeros.Any(x => x.Value.IsInactive))
        {
            Entity inactiveEnemy = enemyHeros.First(x => x.Value.IsInactive).Value.Current;

            if (myCurrentHero.x != inactiveEnemy.x || myCurrentHero.y != inactiveEnemy.y)
            {
                Console.WriteLine("MOVE " + inactiveEnemy.x + " " + inactiveEnemy.y);
            }
            else
            {
                Console.WriteLine("ATTACK " + inactiveEnemy.unitId);
            }
        }
        else if (util.IsMyHeroInRangeOfTheirTower())
        {
            var bush = util.FindNearestBushToMyTower();
            Console.WriteLine("MOVE " + bush.x + " " + bush.y);
        }
        else if (((double)myCurrentHero.health / myCurrentHero.maxHealth) < 0.33)
        {
            var bush = util.FindNearestBushToMyTower();
            if (myCurrentHero.x == bush.x && myCurrentHero.y == bush.y)
            {
                if (potionToBuy.Any())
                {
                    var itemToBuy = potionToBuy.OrderByDescending(x => x.health).First();
                    Console.WriteLine("BUY " + itemToBuy.itemName);
                    util.gold -= itemToBuy.itemCost;
                }
                else
                {
                    Console.WriteLine("WAIT");
                }
            }
            else
            {
                Console.WriteLine("MOVE " + bush.x + " " + bush.y);
            }
        }
        else if (previousHero != null && previousHero.health > myCurrentHero.health)
        {
            var spellToUse = myHero.Spells.FirstOrDefault(x =>
                x.Type == "Defensive" && myCurrentHero.coolDowns[x.CoolDownSlot] == 0 &&
                myCurrentHero.mana >= x.ManaCost);

            if (spellToUse != null)
            {
                spellToUse.Execute();
            }
            else
            {
                var bush = util.FindNearestBushToMyTower();
                Console.WriteLine("MOVE " + bush.x + " " + bush.y);
            }
        }
        else if (myCurrentHero.itemsOwned < 3 && affordableItems.Any())
        {
            var itemToBuy = affordableItems.First();
            Console.WriteLine("BUY " + itemToBuy.itemName);
            util.gold -= itemToBuy.itemCost;
        }
        else if (potionToBuy.Any())
        {
            var itemToBuy = potionToBuy.OrderByDescending(x => x.health).First();
            Console.WriteLine("BUY " + itemToBuy.itemName);
            util.gold -= itemToBuy.itemCost;
        }
        else
        {
            FindTarget(util);
        }
    }

    private static void FindTarget(Util util)
    {
        var myHero = util.Hero;
        var myCurrentHero = util.myCurrentHero;
        var eHeros = util.FindEHeroNotInRangeOfTheirTower();
        if (eHeros.Any())
        {
            Entity eHero = util.FindEHeroInRange(eHeros);
            if (eHero != null)
            {
                util.AttackEHero(util, eHero);
            }
            else
            {
                util.FindNonHerosToAttack();
            }
        }
        else
        {
            util.FindNonHerosToAttack();
        }
    }

    class Util
    {
        public Entity myCurrentHero { get; set; }
        public Entity myPreviousHero { get; set; }
        private List<Bush> bushes { get; set; }
        private List<Entity> entities { get; set; }
        private int myTeam { get; set; }
        private List<Item> shop { get; set; }
        public int gold { get; set; }
        public Hero Hero { get; set; }

        public void UpdateWithRoundInfo(List<Bush> bushes, string heroName, List<Entity> entities, int myTeam,
            List<Item> shop, int gold, Hero hero)
        {
            this.bushes = bushes;
            this.entities = entities;
            this.myTeam = myTeam;
            this.shop = shop;
            this.gold = gold;
            this.myPreviousHero = hero.Previous;
            this.Hero = hero;
            this.myCurrentHero = FindMyHero(heroName);
        }


        public void AttackEHero(Util util, Entity eHero)
        {
            var spellsToUse = Hero.Spells.Where(x =>
                x.Type == "Offensive" && myCurrentHero.coolDowns[x.CoolDownSlot] == 0 &&
                myCurrentHero.mana >= x.ManaCost);
            var spellUsed = false;
            foreach (var spell in spellsToUse)
            {
                if (spell.Range > util.Hypotenuse(Math.Abs(eHero.x - myCurrentHero.x),
                        Math.Abs(eHero.y - myCurrentHero.y)))
                {
                    if (spell.Type == "AOE")
                    {
                        spell.Execute(eHero.x.ToString(), eHero.y.ToString());
                        spellUsed = true;
                    }
                    else if (spell.Type == "Unit")
                    {
                        spell.Execute(eHero.unitId.ToString());
                        spellUsed = true;
                    }
                }
            }

            if (!spellUsed)
            {
                Console.WriteLine("ATTACK " + eHero.unitId);
            }
        }

        public Bush FindNearestBushToMe()
        {
            return bushes.OrderBy(bush =>
                Hypotenuse(Math.Abs(myCurrentHero.x - bush.x), Math.Abs(myCurrentHero.y - bush.y))).First();
        }

        public Entity FindMyTower()
        {
            return entities.First(x => x.unitType == "TOWER" && x.team == myTeam);
        }

        public Entity FindMyHero(string heroName)
        {
            return entities.FirstOrDefault(x => x.heroType != "-" && x.team == myTeam && x.heroType == heroName);
        }

        public List<Item> FindAffordableWeapon()
        {
            var findAffordableWeapon = shop.Where(x => x.itemCost <= gold && x.damage > 0)
                .OrderBy(x => x.GoldPerDamageRating)
                .ToList();
            foreach (var a in findAffordableWeapon)
            {
                Console.Error.WriteLine(a);
            }

            return findAffordableWeapon;
        }

        public List<Entity> FindEHeroNotInRangeOfTheirTower()
        {
            var eTower = FindETower();
            var heros = new List<Entity>();
            foreach (var hero in entities.Where(x => x.heroType != "-" && x.team != myTeam))
            {
                if (eTower.attackRange + 500 < Hypotenuse(Math.Abs(eTower.x - hero.x), Math.Abs(eTower.y - hero.y)))
                {
                    heros.Add(hero);
                }
            }

            return heros;
        }

        private Entity FindETower()
        {
            return entities.First(e => e.unitType == "TOWER" && e.team != myTeam);
        }

        public bool IsMyHeroInRangeOfTheirTower()
        {
            var eTower = FindETower();
            return eTower.attackRange + 300 >
                   Hypotenuse(Math.Abs(eTower.x - myCurrentHero.x), Math.Abs(eTower.y - myCurrentHero.y));
        }

        public Bush FindNearestBushToMyTower()
        {
            var eTower = entities.First(e => e.unitType == "TOWER" && e.team == myTeam);
            foreach (var bush in bushes)
            {
                if (eTower.attackRange > Hypotenuse(Math.Abs(eTower.x - bush.x), Math.Abs(eTower.y - bush.y)))
                {
                    return bush;
                }
            }

            return bushes.FirstOrDefault();
        }

        public List<Entity> FindMyUnitsWhichAreInRangeOfTheirTower()
        {
            var eTower = FindETower();
            var myUnits = new List<Entity>();
            foreach (var myUnit in entities.Where(x => x.team == myTeam && x.unitType == "UNIT"))
            {
                if (eTower.attackRange > Hypotenuse(Math.Abs(eTower.x - myUnit.x), Math.Abs(eTower.y - myUnit.y)))
                {
                    myUnits.Add(myUnit);
                }
            }

            return myUnits;
        }

        public List<Entity> FindEnemysWhichInRangeOfMyTower()
        {
            var mTower = entities.First(e => e.unitType == "TOWER" && e.team == myTeam);
            var eUnits = new List<Entity>();
            foreach (var eUnit in entities.Where(x => x.team != myTeam && x.unitType != "GROOT"))
            {
                if (mTower.attackRange > Hypotenuse(Math.Abs(mTower.x - eUnit.x), Math.Abs(mTower.y - eUnit.y)))
                {
                    eUnits.Add(eUnit);
                }
            }

            return eUnits;
        }

        public List<Item> GetPotionsIfNeeded()
        {
            return shop.Where(x => x.isPotion == 1 && x.health > 0 && x.itemCost < gold && myCurrentHero.IsLowHealth)
                .ToList();
        }

        public double Hypotenuse(double side1, double side2)
        {
            return Math.Sqrt(side1 * side1 + side2 * side2);
        }

        public Entity FindEHeroInRange(List<Entity> eHeros)
        {
            return eHeros.Where(IsInRangeOfMyHero).OrderBy(x => x.health).FirstOrDefault();
        }

        public void FindNonHerosToAttack()
        {
            Console.Error.WriteLine("Inside here?");
            var target = FindLowestHealthEnemyWithinRangeOfHero() ?? FindLowestHealthAllyWithinRangeOfHero();
            if (target != null)
            {
                Console.WriteLine("ATTACK " + target.unitId);
            }
            else
            {
                var furestAlly = FurthestAlly();
                if (furestAlly != null)
                {
                    Console.WriteLine("MOVE " + furestAlly.x + " " + furestAlly.y + "; Moving with units");
                }
                else
                {
                    var bush = FindNearestBushToMyTower();
                    Console.WriteLine("MOVE " + bush.x + " " + bush.y);
                }

            }
        }

        private Entity FurthestAlly()
        {
            var eTower = FindETower();
            return entities.Where(x => x.team == myTeam && x.unitType == "UNIT").OrderBy(x =>
                Hypotenuse(Math.Abs(x.x - eTower.x), Math.Abs(x.y - eTower.y))).FirstOrDefault();
        }

        private Entity FindLowestHealthEnemyWithinRangeOfHero()
        {
            return entities.Where(x => x.team != myTeam && x.health < 60 && IsInRangeOfMyHero(x)).OrderBy(x => x.health).FirstOrDefault();
        }

        private Entity FindLowestHealthAllyWithinRangeOfHero()
        {
            return entities.Where(x => x.team == myTeam && x.health < 60 && IsInRangeOfMyHero(x)).OrderBy(x => x.health).FirstOrDefault();
        }

        private bool IsInRangeOfMyHero(Entity target)
        {
            return myCurrentHero.attackRange + myCurrentHero.movementSpeed > Hypotenuse(
                       Math.Abs(target.x - myCurrentHero.x),
                       Math.Abs(target.y - myCurrentHero.y));
        }
    }

    class Entity
    {
        public int unitId { get; set; }
        public int team { get; set; }
        public string unitType { get; set; }
        public int x { get; set; }
        public int y { get; set; }
        public int attackRange { get; set; }
        public int health { get; set; }
        public int maxHealth { get; set; }
        public int shield { get; set; }
        public int attackDamage { get; set; }
        public int movementSpeed { get; set; }
        public int stunDuration { get; set; }
        public int goldValue { get; set; }
        public Dictionary<string, int> coolDowns { get; set; }
        public int countDown1 { get; set; }
        public int countDown2 { get; set; }
        public int countDown3 { get; set; }
        public int mana { get; set; }
        public int maxMana { get; set; }
        public int manaRegeneration { get; set; }
        public string heroType { get; set; }
        public int isVisible { get; set; }
        public int itemsOwned { get; set; }

        public Entity(string[] inputs)
        {
            unitId = int.Parse(inputs[0]);
            team = int.Parse(inputs[1]);
            unitType = inputs[2]; // UNIT, HERO, TOWER, can also be GROOT from wood1
            x = int.Parse(inputs[3]);
            y = int.Parse(inputs[4]);
            attackRange = int.Parse(inputs[5]);
            health = int.Parse(inputs[6]);
            maxHealth = int.Parse(inputs[7]);
            shield = int.Parse(inputs[8]); // useful in bronze
            attackDamage = int.Parse(inputs[9]);
            movementSpeed = int.Parse(inputs[10]);
            stunDuration = int.Parse(inputs[11]); // useful in bronze
            goldValue = int.Parse(inputs[12]);
            countDown1 = int.Parse(inputs[13]); // all countDown and mana variables are useful starting in bronze
            countDown2 = int.Parse(inputs[14]);
            countDown3 = int.Parse(inputs[15]);
            coolDowns = new Dictionary<string, int>()
            {
                {"1", int.Parse(inputs[13])},
                {"2", int.Parse(inputs[14])},
                {"3", int.Parse(inputs[15])}
            };
            mana = int.Parse(inputs[16]);
            maxMana = int.Parse(inputs[17]);
            manaRegeneration = int.Parse(inputs[18]);
            heroType = inputs[19]; // DEADPOOL, VALKYRIE, DOCTOR_STRANGE, HULK, IRONMAN
            isVisible = int.Parse(inputs[20]); // 0 if it isn't
            itemsOwned = int.Parse(inputs[21]); // useful from wood1
        }

        public bool IsLowHealth
        {
            get { return health / maxHealth < 0.5; }
        }
    }

    class Item
    {
        public string itemName { get; set; }
        public int itemCost { get; set; }
        public int damage { get; set; }
        public int health { get; set; }
        public int maxHealth { get; set; }
        public int mana { get; set; }
        public int maxMana { get; set; }
        public int moveSpeed { get; set; }
        public int manaRegeneration { get; set; }
        public int isPotion { get; set; }

        public Item(string[] inputs)
        {
            itemName = inputs[
                0]; // contains keywords such as BRONZE, SILVER and BLADE, BOOTS connected by "_" to help you sort easier
            itemCost = int.Parse(inputs[1]); // BRONZE items have lowest cost, the most expensive items are LEGENDARY
            damage = int.Parse(inputs[2]); // keyword BLADE is present if the most important item stat is damage
            health = int.Parse(inputs[3]);
            maxHealth = int.Parse(inputs[4]);
            mana = int.Parse(inputs[5]);
            maxMana = int.Parse(inputs[6]);
            moveSpeed = int.Parse(inputs[7]); // keyword BOOTS is present if the most important item stat is moveSpeed
            manaRegeneration = int.Parse(inputs[8]);
            isPotion = int.Parse(inputs[9]); // 0 if it's not instantly consumed
        }

        public double GoldPerDamageRating
        {
            get { return itemCost / (double)damage; }
        }
    }

    class Hero
    {
        public string Name { get; set; }
        public Entity Current { get; set; }
        public Entity Previous { get; set; }
        public List<Item> Items { get; set; }
        public List<Spell> Spells { get; set; }
        public int DefaultY { get; set; }
        public int NoOfTurnsSinceLastMove { get; set; }

        public bool IsInactive
        {
            get { return NoOfTurnsSinceLastMove > 3; }
        }

        public Hero(string Name, int Y)
        {
            this.Name = Name;
            this.DefaultY = Y;
            Items = new List<Item>();

            if (Name == "HULK") Spells = GetHulkSpells();
            else if (Name == "DEADPOOL") Spells = GetDeadPoolSpells();
        }

        public Hero()
        {
        }

        private static List<Spell> GetDeadPoolSpells()
        {
            return new List<Spell>
            {
                new Spell
                {
                    Name = "COUNTER",
                    ManaCost = 40,
                    CoolDownSlot = "1",
                    Range = 350,
                    Type = "Defensive",
                    Target = "Self"
                },
                new Spell
                {
                    Name = "WIRE {0} {1]",
                    ManaCost = 50,
                    CoolDownSlot = "2",
                    Range = 200,
                    Type = "Offensive",
                    Target = "AOE"
                }
            };
        }

        private static List<Spell> GetHulkSpells()
        {
            return new List<Spell>
            {
                new Spell
                {
                    Name = "CHARGE {0}",
                    ManaCost = 20,
                    CoolDownSlot = "1",
                    Range = 300,
                    Type = "Offensive",
                    Target = "Unit"
                },
                new Spell
                {
                    Name = "EXPLOSIVESHIELD",
                    ManaCost = 30,
                    CoolDownSlot = "2",
                    Range = 0,
                    Type = "Defensive",
                    Target = "Self"
                }
            };
        }
    }

    class Spell
    {
        public string Name { get; set; }
        public int ManaCost { get; set; }
        public int Range { get; set; }
        public string CoolDownSlot { get; set; }
        public string Type { get; set; }
        public string Target { get; set; }

        public void Execute(params string[] args)
        {
            Console.WriteLine(string.Format(Name, args));
        }
    }

    class Bush
    {
        public int x { get; set; }
        public int y { get; set; }

        public Bush(string[] inputs)
        {
            x = int.Parse(inputs[1]);
            y = int.Parse(inputs[2]);
        }
    }
}
