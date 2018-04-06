using System;
using System.Collections.Generic;
using System.Linq;

class Player
{
    static void Main_Old(string[] args)
    {
        string[] inputs;
        int myTeam = int.Parse(Console.ReadLine());
        int bushAndSpawnPointCount = int.Parse(Console.ReadLine()); // useful from wood1, represents the number of bushes and the number of places where neutral units can spawn

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

        List<Hero> myHeros = new List<Hero>{
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
            int roundType = int.Parse(Console.ReadLine()); // a positive value will show the number of heroes that await a command
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
                    hero.Current = FindMyHero(entities, myTeam, hero.Name);
                    if (hero.Current != null)
                    {
                        HeroCommands(entities, ref gold, shop, myTeam, hero, bushes, enemyHeros);
                    }

                    hero.Previous = hero.Current;
                }
            }

            roundNo++;
        }
    }

    private static void HeroCommands(List<Entity> entities, ref int gold, List<Item> shop, int myTeam, Hero hero, List<Bush> bushes, Dictionary<string, Hero> enemyHeros)
    {
        Entity myHero = hero.Current;
        var affordableItems = FindAffordableWeapon(shop, gold);
        var potionToBuy = GetPotionsIfNeeded(shop, gold);
        //  && !AreEnemyInRangeOfMyTower(entities, myTeam).Any()
        if (enemyHeros.Any(x => x.Value.IsInactive))
        {
            Entity inactiveEnemy = enemyHeros.First(x => x.Value.IsInactive).Value.Current;

            if (myHero.x != inactiveEnemy.x || myHero.y != inactiveEnemy.y)
            {
                Console.WriteLine("MOVE " + inactiveEnemy.x + " " + inactiveEnemy.y);
            }
            else
            {
                Console.WriteLine("ATTACK " + inactiveEnemy.unitId);
            }
        }
        else if (((double)hero.Current.health / hero.Current.maxHealth) < 0.33)
        {
            var bush = FindNearestBushToMyTower(entities, myTeam, bushes);
            if (myHero.x == bush.x && myHero.y == bush.y)
            {
                if (potionToBuy.Any())
                {
                    var itemToBuy = potionToBuy.OrderByDescending(x => x.health).First();
                    Console.WriteLine("BUY " + itemToBuy.itemName);
                    gold -= itemToBuy.itemCost;
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
        else if (hero.Previous != null && hero.Previous.health > hero.Current.health)
        {
            var spellToUse = hero.Spells.Where(x => x.Type == "Defensive" && hero.Current.coolDowns[x.CoolDownSlot] == 0 && hero.Current.mana >= x.ManaCost).FirstOrDefault();

            if (spellToUse != null)
            {
                spellToUse.Execute();
            }
            else
            {
                var bush = FindNearestBushToMyTower(entities, myTeam, bushes);
                Console.WriteLine("MOVE " + bush.x + " " + bush.y);
            }
        }
        else if (myHero.itemsOwned < 3 && affordableItems.Any())
        {
            var itemToBuy = affordableItems.First();
            Console.WriteLine("BUY " + itemToBuy.itemName);
            gold -= itemToBuy.itemCost;
        }
        else if (potionToBuy.Any())
        {
            var itemToBuy = potionToBuy.OrderByDescending(x => x.health).First();
            Console.WriteLine("BUY " + itemToBuy.itemName);
            gold -= itemToBuy.itemCost;
        }
        else
        {
            var eHeros = AreOtherHerosNotInRangeOfTheirTower(entities, myTeam, myTeam);
            if (eHeros.Any())
            {
                var spellsToUse = hero.Spells.Where(x => x.Type == "Offensive" && hero.Current.coolDowns[x.CoolDownSlot] == 0 && hero.Current.mana >= x.ManaCost);
                var eHero = eHeros.First();
                var spellUsed = false;
                foreach (var spell in spellsToUse)
                {
                    if (spell.Range > Hypotenuse(Math.Abs(eHero.x - myHero.x), Math.Abs(eHero.y - myHero.y)))
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
            else
            {
                FindTarget(entities, myTeam);
            }
        }
    }

    private static Bush FindNearestBushToMe(List<Bush> bushes, Entity hero)
    {
        return bushes.OrderBy(bush => Hypotenuse(Math.Abs(hero.x - bush.x), Math.Abs(hero.y - bush.y))).First();
    }

    private static Entity FindMyTower(List<Entity> entities, int myTeam)
    {
        return entities.Where(x => x.unitType == "TOWER" && x.team == myTeam).First();
    }

    private static Entity FindMyHero(List<Entity> entities, int myTeam, string heroName)
    {
        return entities.Where(x => x.heroType != "-" && x.team == myTeam && x.heroType == heroName).FirstOrDefault();
    }

    private static List<Item> FindAffordableWeapon(List<Item> shop, int gold)
    {
        var findAffordableWeapon = shop.Where(x => x.itemCost <= gold && x.damage > 0).OrderBy(x => x.GoldPerDamageRating)
            .ToList();
        foreach (var a in findAffordableWeapon)
        {
            Console.Error.WriteLine(a);
        }

        return findAffordableWeapon;
    }

    private static void FindTarget(List<Entity> entities, int myTeam)
    {
        var myHeros = AreOtherHerosNotInRangeOfTheirTower(entities, myTeam - 1, myTeam);
        if (myHeros.Any())
        {
            if (AreMyTeamInRangeOfTheirTower(entities, myTeam).Any())
            {
                var otherTeam = entities.Where(e => e.team != myTeam && e.unitType != "GROOT");
                if (otherTeam.Any(x => x.unitType == "UNIT"))
                    Console.WriteLine("ATTACK_NEAREST UNIT");
                else
                    Console.WriteLine("ATTACK " + entities.Where(e => e.team != myTeam && e.unitType != "HERO").First().unitId);
            }
            else
            {
                var myHero = entities.Where(x => x.heroType != "-" && x.team == myTeam).First();
                var myTower = entities.Where(x => x.unitType == "TOWER" && x.team == myTeam).First();
                Console.WriteLine("MOVE " + myTower.x + " " + myHero.y);
            }
        }
        else
        {
            var otherTeam = entities.Where(e => e.team != myTeam && e.unitType != "GROOT");
            if (otherTeam.Any(x => x.unitType == "UNIT"))
                Console.WriteLine("ATTACK_NEAREST UNIT");
            else
                Console.WriteLine("ATTACK " + entities.Where(e => e.team != myTeam && e.unitType != "HERO").First().unitId);
        }
    }

    private static List<Entity> AreOtherHerosNotInRangeOfTheirTower(List<Entity> entities, int heroTeam, int towerTeam)
    {
        var eTower = entities.Where(e => e.unitType == "TOWER" && e.team != towerTeam).First();
        var heros = new List<Entity>();
        foreach (var hero in entities.Where(x => x.heroType != "-" && x.team != heroTeam))
        {
            if (eTower.attackRange > Hypotenuse(Math.Abs(eTower.x - hero.x), Math.Abs(eTower.y - hero.y)))
            {
                heros.Add(hero);
            }
        }

        return heros;
    }

    private static Bush FindNearestBushToMyTower(List<Entity> entities, int myTeam, List<Bush> bushes)
    {
        var eTower = entities.Where(e => e.unitType == "TOWER" && e.team == myTeam).First();
        foreach (var bush in bushes)
        {
            if (eTower.attackRange > Hypotenuse(Math.Abs(eTower.x - bush.x), Math.Abs(eTower.y - bush.y)))
            {
                return bush;
            }
        }

        return bushes.FirstOrDefault();
    }

    private static List<Entity> AreMyTeamInRangeOfTheirTower(List<Entity> entities, int myTeam)
    {
        var eTower = entities.Where(e => e.unitType == "TOWER" && e.team != myTeam).First();
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

    private static List<Entity> AreEnemyInRangeOfMyTower(List<Entity> entities, int myTeam)
    {
        var mTower = entities.Where(e => e.unitType == "TOWER" && e.team == myTeam).First();
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

    private static List<Item> GetPotionsIfNeeded(List<Item> shopItems, int currentGold)
    {
        return shopItems.Where(x => x.isPotion == 1 && x.health > 0 && x.itemCost < currentGold).ToList();
    }

    static double Hypotenuse(double side1, double side2)
    {
        return Math.Sqrt(side1 * side1 + side2 * side2);
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
                {"1",    int.Parse(inputs[13])},
                {"2",    int.Parse(inputs[14])},
                {"3",    int.Parse(inputs[15])}
            };
        mana = int.Parse(inputs[16]);
        maxMana = int.Parse(inputs[17]);
        manaRegeneration = int.Parse(inputs[18]);
        heroType = inputs[19]; // DEADPOOL, VALKYRIE, DOCTOR_STRANGE, HULK, IRONMAN
        isVisible = int.Parse(inputs[20]); // 0 if it isn't
        itemsOwned = int.Parse(inputs[21]); // useful from wood1
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
        itemName = inputs[0]; // contains keywords such as BRONZE, SILVER and BLADE, BOOTS connected by "_" to help you sort easier
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

    public override string ToString()
    {
        return string.Join(" | ", itemName, itemCost.ToString(), damage.ToString());
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
        get
        {
            return NoOfTurnsSinceLastMove > 3;
        }
    }

    public Hero(string Name, int Y)
    {
        this.Name = Name;
        this.DefaultY = Y;
        Items = new List<Item>();

        if (Name == "HULK") Spells = GetHulkSpells();
        else if (Name == "DEADPOOL") Spells = GetDeadPoolSpells();
    }

    public Hero() { }

    private static List<Spell> GetDeadPoolSpells()
    {
        return new List<Spell>{
                new Spell {
                    Name = "COUNTER",
                    ManaCost = 40,
                    CoolDownSlot = "1",
                    Range = 350,
                    Type = "Defensive",
                    Target = "Self"
                },
                new Spell {
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
        return new List<Spell>{
                new Spell {
                    Name = "CHARGE {0}",
                    ManaCost = 20,
                    CoolDownSlot = "1",
                    Range = 300,
                    Type = "Offensive",
                    Target = "Unit"
                },
                new Spell {
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
        string entityType = inputs[0]; // BUSH, from wood1 it can also be SPAWN
        x = int.Parse(inputs[1]);
        y = int.Parse(inputs[2]);
        int radius = int.Parse(inputs[3]);
    }
}

/*
using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
class Player
{
    static void Main(string[] args)
    {
        List<ShopItem> shopItems = new List<ShopItem>();
        string[] inputs;
        int myTeam = int.Parse(Console.ReadLine());
        int bushAndSpawnPointCount = int.Parse(Console.ReadLine()); // useful from wood1, represents the number of bushes and the number of places where neutral units can spawn
        for (int i = 0; i < bushAndSpawnPointCount; i++)
        {
            inputs = Console.ReadLine().Split(' ');
            string entityType = inputs[0]; // BUSH, from wood1 it can also be SPAWN
            int x = int.Parse(inputs[1]);
            int y = int.Parse(inputs[2]);
            int radius = int.Parse(inputs[3]);
        }
        int itemCount = int.Parse(Console.ReadLine()); // useful from wood2
        for (int i = 0; i < itemCount; i++)
        {
            shopItems.Add(new ShopItem(Console.ReadLine().Split(' ')));
        }

        bool? isLeftToRight = null;

        List<Hero> myHeros = new List<Hero>{
                new Hero("IRONMAN", 400),
                new Hero("DOCTOR_STRANGE", 600)
            };

        // game loop
        while (true)
        {
            int gold = int.Parse(Console.ReadLine());
            int enemyGold = int.Parse(Console.ReadLine());
            int roundType = int.Parse(Console.ReadLine()); // a positive value will show the number of heroes that await a command
            int entityCount = int.Parse(Console.ReadLine());
            List<Entity> entities = new List<Entity>();

            for (int i = 0; i < entityCount; i++)
            {
                inputs = Console.ReadLine().Split(' ');
                var entity = new Entity(inputs);
                entities.Add(entity);

                if (!isLeftToRight.HasValue && entity.IsMyHero(myTeam))
                {
                    isLeftToRight = IsLeftToRight(entity);
                }
            }

            // Write an action using Console.WriteLine()
            // To debug: Console.Error.WriteLine("Debug messages...");
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
                //             bool kiting = true;
                //if (kiting)
                //{
                //    var herosAlive = myHeros.Where(hero => FindMyHero(entities, myTeam, hero.Name) != null);
                //    var groots = entities.Where(x => x.Team == -1);
                //    var grootTarget = isLeftToRight.HasValue && isLeftToRight.Value ? groots.OrderBy(x => x.X).FirstOrDefault() :
                //        groots.OrderByDescending(x => x.X).FirstOrDefault();

                //    if(grootTarget != null)
                //    {
                //        Console.WriteLine("ATTACK " + grootTarget.UnitId);
                //        Console.WriteLine("ATTACK " + grootTarget.UnitId);
                //        continue;
                //    }
                //}

                foreach (var hero in myHeros)
                {
                    hero.Current = FindMyHero(entities, myTeam, hero.Name);
                    if (hero.Current != null)
                    {
                        HeroCommands(shopItems, gold, isLeftToRight, entities, myTeam, hero);
                    }

                    hero.Previous = hero.Current;
                }
            }
        }
    }

    private static void HeroCommands(List<ShopItem> shopItems, int gold, bool? isLeftToRight,
        List<Entity> entities, int myTeam, Hero hero)
    {
        List<ShopItem> bladesToBuy = hero.Name == "IRONMAN" ? GetAffordableBlades(shopItems, gold, hero.Items, 0) : new List<ShopItem>();
        List<ShopItem> expensiveBladesToBuy = GetAffordableBlades(shopItems, gold, hero.Items, 2);
        List<ShopItem> potionToBuy = GetPotionsIfNeeded(shopItems, gold, hero.Current.HealthLost());
        // List<ShopItem> potionToBuy = new List<ShopItem>();

        var healingSpellToUse = hero.Spells.Where(x => hero.Current.HealthLost() > 100 && x.Type == "Healing" && hero.Current.CoolDowns[x.CoolDownSlot] == 0 && hero.Current.Mana >= x.ManaCost).FirstOrDefault();

        var furthestAlly = isLeftToRight.HasValue ? FurthestAlly(isLeftToRight.Value, entities, myTeam) : 0;
        var furthestEnemy = isLeftToRight.HasValue ? FurthestEnemy(isLeftToRight.Value, entities, myTeam) : 0;
        var lowestHpEnemy = FindLowestHealthEnemyWithinRangeOfHero(entities, myTeam, hero.Current);
        var lowestHpAlly = FindLowestHealthAllyWithinRangeOfHero(entities, myTeam, hero.Current);
        var lowestHpEnemyHero = FindLowestHealthEnemyHero(entities, myTeam, hero.Current);
        var locationToMoveTo = isLeftToRight.HasValue ? LocationToMoveToSoWeAreBehindMyUnit(entities, myTeam, isLeftToRight.Value) : -1;
        if (hero.Previous != null && hero.Previous.Health > hero.Current.Health)
        {
            var spellToUse = hero.Spells.Where(x => x.Type == "Defensive" && hero.Current.CoolDowns[x.CoolDownSlot] == 0 && hero.Current.Mana >= x.ManaCost).FirstOrDefault();
            if (spellToUse != null && spellToUse.Target == "AOE")
            {
                var xModifier = isLeftToRight.Value ? (-1 * spellToUse.Range) : spellToUse.Range;
                spellToUse.Execute((hero.Current.X + xModifier).ToString(), hero.DefaultY.ToString());
            }
            else
                Console.WriteLine("MOVE " + (isLeftToRight.Value ? 200 : 1720) + " " + hero.DefaultY.ToString());
        }
        else if (healingSpellToUse != null)
        {
            if (healingSpellToUse.Target == "AOE")
            {
                healingSpellToUse.Execute(hero.Current.X.ToString(), hero.Current.Y.ToString());
            }
            else if (healingSpellToUse.Target == "Single")
            {
                healingSpellToUse.Execute(hero.Current.UnitId.ToString());
            }
        }
        else if (potionToBuy.Any())
        {
            var itemToBuy = potionToBuy.OrderByDescending(x => x.Health).First();
            Console.WriteLine("BUY " + itemToBuy.ItemName);
        }
        else if (hero.Items.Count() < 3 && bladesToBuy.Any())
        {
            var itemToBuy = bladesToBuy.OrderByDescending(x => x.Damage).First();
            hero.Items.Add(itemToBuy);
            Console.WriteLine("BUY " + itemToBuy.ItemName);
        }
        else if (hero.Items.Count() == 3 && expensiveBladesToBuy.Any())
        {
            Console.Error.WriteLine("Buying expensive item!");
            var itemToBuy = expensiveBladesToBuy.OrderBy(x => x.Damage).First();
            hero.Items.Add(itemToBuy);
            Console.WriteLine("BUY " + itemToBuy.ItemName);
        }
        else if (locationToMoveTo >= 0)
        {
            Console.WriteLine("MOVE " + locationToMoveTo + " " + hero.DefaultY.ToString());
        }
        else if (lowestHpEnemy != null)
        {
            var spellToUse = hero.Spells.Where(x => x.Type == "Offensive" && hero.Current.CoolDowns[x.CoolDownSlot] == 0 && hero.Current.Mana >= x.ManaCost).FirstOrDefault();
            if (spellToUse != null && spellToUse.Target == "AOE")
                spellToUse.Execute(lowestHpEnemy.X.ToString(), lowestHpEnemy.Y.ToString());
            else
                Console.WriteLine("ATTACK " + lowestHpEnemy.UnitId);
        }
        else if (lowestHpAlly != null)
        {
            Console.WriteLine("ATTACK " + lowestHpAlly.UnitId);
        }
        else
        {
            var nearestEnemyHero = FindEnemyHeroWithinRangeOfHero(entities, myTeam, hero.Current);

            if (nearestEnemyHero != null) Console.WriteLine("ATTACK " + nearestEnemyHero.UnitId);
            else
            {
                if ((isLeftToRight.HasValue && isLeftToRight.Value && furthestEnemy > 1000) ||
                    (isLeftToRight.HasValue && !isLeftToRight.Value && furthestEnemy < 1000))
                {
                    Console.WriteLine("WAIT");
                }
                else
                    Console.WriteLine("ATTACK_NEAREST UNIT");
            }
        }
    }

    private static List<ShopItem> GetAffordableItems(List<ShopItem> shopItems, int currentGold)
    {
        return shopItems.Where(x => x.ItemCost < currentGold).ToList();
    }

    private static List<ShopItem> GetAffordableBlades(List<ShopItem> shopItems, int currentGold, List<ShopItem> itemsOwned, int costModifier)
    {
        int mostExpensiveItemOwned = int.MinValue;
        if (itemsOwned.Any())
        {
            mostExpensiveItemOwned = itemsOwned.Max(x => x.ItemCost) * costModifier;
        }
        return shopItems.Where(x => x.ItemName.ToLowerInvariant().Contains("blade") && x.Damage > 0 && x.ItemCost < currentGold && x.ItemCost > mostExpensiveItemOwned).ToList();
    }

    private static List<ShopItem> GetPotionsIfNeeded(List<ShopItem> shopItems, int currentGold, int lostHealth)
    {
        return shopItems.Where(x => x.IsPotion == 1 && x.Health > 0 && x.ItemCost < currentGold && lostHealth > x.Health).ToList();
    }

    private static Entity FindMyHero(List<Entity> entities, int myTeamId, string heroName)
    {
        return entities.Where(x => x.HeroType != "-" && x.Team == myTeamId && x.HeroType == heroName).FirstOrDefault();
    }

    private static bool? IsLeftToRight(Entity myHero)
    {
        return myHero.X == 200 ? true : (myHero.X == 1720 ? false : (bool?)null);
    }

    private static int FurthestAlly(bool isLeftToRight, List<Entity> entities, int myTeamId)
    {
        return isLeftToRight
            ? entities.Where(x => x.Team == myTeamId && x.HeroType == "-").Max(x => x.X)
            : entities.Where(x => x.Team == myTeamId && x.HeroType == "-").Min(x => x.X);
    }

    private static int FurthestEnemy(bool isLeftToRight, List<Entity> entities, int myTeamId)
    {
        return isLeftToRight
            ? entities.Where(x => x.Team >= 0 && x.Team != myTeamId).Min(x => x.X)
            : entities.Where(x => x.Team >= 0 && x.Team != myTeamId).Max(x => x.X);
    }

    private static Entity FindLowestHealthEnemyWithinRangeOfHero(List<Entity> entities, int myTeam, Entity hero)
    {
        List<int> heroRanges = new List<int> { hero.AttackRange - hero.X + 10, hero.AttackRange + hero.X - 10 };
        var minRange = heroRanges.Min();
        var maxRange = heroRanges.Max();
        return entities.Where(x => x.Team != myTeam && x.Health < 60 && x.X > minRange && x.X < maxRange).OrderBy(x => x.Health).FirstOrDefault();
    }

    private static Entity FindEnemyHeroWithinRangeOfHero(List<Entity> entities, int myTeam, Entity hero)
    {
        List<int> heroRanges = new List<int> { hero.AttackRange - hero.X + 10, hero.AttackRange + hero.X - 10 };
        var minRange = heroRanges.Min();
        var maxRange = heroRanges.Max();
        return entities.Where(x => x.Team != myTeam && x.HeroType != "-" && x.X > minRange && x.X < maxRange).OrderBy(x => x.Health).FirstOrDefault();
    }

    private static Entity FindLowestHealthAllyWithinRangeOfHero(List<Entity> entities, int myTeam, Entity hero)
    {
        List<int> heroRanges = new List<int> { hero.AttackRange - hero.X + 10, hero.AttackRange + hero.X - 10 };
        var minRange = heroRanges.Min();
        var maxRange = heroRanges.Max();
        return entities.Where(x => x.Team == myTeam && x.Health < 60 && x.X > minRange && x.X < maxRange).OrderBy(x => x.Health).FirstOrDefault();
    }

    private static Entity FindLowestHealthEnemyHero(List<Entity> entities, int myTeam, Entity hero)
    {
        return entities.Where(x => x.Team != myTeam && x.HeroType != "-").OrderBy(x => x.Health).FirstOrDefault();
    }

    private static int LocationToMoveToSoWeAreBehindMyUnit(List<Entity> entities, int myTeam, bool isLeftToRight)
    {
        var allyPosition = FurthestAlly(isLeftToRight, entities, myTeam);
        List<int> unitLocations = new List<int> { allyPosition, FurthestEnemy(isLeftToRight, entities, myTeam) };
        var minLocation = unitLocations.Min();
        var maxLocation = unitLocations.Max();
        var retVal = maxLocation - minLocation > 150 ?
            isLeftToRight ?
                (allyPosition > 150 ? allyPosition - 150 : 0) :
                (allyPosition < 1520 ? allyPosition + 150 : allyPosition) :
                -1;
        return isLeftToRight ? retVal > 1000 ? 1000 : retVal : retVal < 1000 ? 1000 : retVal;
    }
}

class ShopItem
{
    public string ItemName { get; set; }
    public int ItemCost { get; set; }
    public int Damage { get; set; }
    public int Health { get; set; }
    public int MaxHealth { get; set; }
    public int Mana { get; set; }
    public int MaxMana { get; set; }
    public int MoveSpeed { get; set; }
    public int ManaRegeneration { get; set; }
    public int IsPotion { get; set; }

    public ShopItem(string[] inputs)
    {
        ItemName = inputs[0]; // contains keywords such as BRONZE, SILVER and BLADE, BOOTS connected by "_" to help you sort easier
        ItemCost = int.Parse(inputs[1]); // BRONZE items have lowest cost, the most expensive items are LEGENDARY
        Damage = int.Parse(inputs[2]); // keyword BLADE is present if the most important item stat is damage
        Health = int.Parse(inputs[3]);
        MaxHealth = int.Parse(inputs[4]);
        Mana = int.Parse(inputs[5]);
        MaxMana = int.Parse(inputs[6]);
        MoveSpeed = int.Parse(inputs[7]); // keyword BOOTS is present if the most important item stat is moveSpeed
        ManaRegeneration = int.Parse(inputs[8]);
        IsPotion = int.Parse(inputs[9]); // 0 if it's not instantly consumed

        Console.Error.WriteLine("Item Name: " + ItemName);
        Console.Error.WriteLine("Item Cost: " + ItemCost);
        Console.Error.WriteLine("Damage: " + Damage);
        Console.Error.WriteLine("Health: " + Health);
        Console.Error.WriteLine("Max Health: " + MaxHealth);
        Console.Error.WriteLine("Mana: " + Mana);
        Console.Error.WriteLine("Max Mana: " + MaxMana);
        Console.Error.WriteLine("MoveSpeed: " + MoveSpeed);
    }

    public int DamageToCost
    {
        get
        {
            if (Damage == 0) return ItemCost;
            return ItemCost / Damage;
        }
    }

    public int MaxHealthToCost
    {
        get
        {
            if (MaxHealth == 0) return ItemCost;
            return ItemCost / MaxHealth;
        }
    }
}

class Entity
{
    public int UnitId { get; set; }
    public int Team { get; set; }
    public string UnitType { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int AttackRange { get; set; }
    public int Health { get; set; }
    public int MaxHealth { get; set; }
    public int Shield { get; set; }
    public int AttackDamage { get; set; }
    public int MovementSpeed { get; set; }
    public int StunDuration { get; set; }
    public int GoldValue { get; set; }
    public Dictionary<string, int> CoolDowns { get; set; }
    public int CountDown1 { get; set; }
    public int CountDown2 { get; set; }
    public int CountDown3 { get; set; }
    public int Mana { get; set; }
    public int MaxMana { get; set; }
    public int ManaRegeneration { get; set; }
    public string HeroType { get; set; }
    public int IsVisible { get; set; }
    public int ItemsOwned { get; set; }

    public Entity(string[] inputs)
    {
        UnitId = int.Parse(inputs[0]);
        Team = int.Parse(inputs[1]);
        UnitType = inputs[2]; // UNIT, HERO, TOWER, can also be GROOT from wood1
        X = int.Parse(inputs[3]);
        Y = int.Parse(inputs[4]);
        AttackRange = int.Parse(inputs[5]);
        Health = int.Parse(inputs[6]);
        MaxHealth = int.Parse(inputs[7]);
        Shield = int.Parse(inputs[8]); // useful in bronze
        AttackDamage = int.Parse(inputs[9]);
        MovementSpeed = int.Parse(inputs[10]);
        StunDuration = int.Parse(inputs[11]); // useful in bronze
        GoldValue = int.Parse(inputs[12]);
        CoolDowns = new Dictionary<string, int>()
            {
                {"1",    int.Parse(inputs[13])},
                {"2",    int.Parse(inputs[14])},
                {"3",    int.Parse(inputs[15])}
            };
        CountDown1 = int.Parse(inputs[13]); // all countDown and mana variables are useful starting in bronze
        CountDown2 = int.Parse(inputs[14]);
        CountDown3 = int.Parse(inputs[15]);
        Mana = int.Parse(inputs[16]);
        MaxMana = int.Parse(inputs[17]);
        ManaRegeneration = int.Parse(inputs[18]);
        HeroType = inputs[19]; // DEADPOOL, VALKYRIE, DOCTOR_STRANGE, HULK, IRONMAN
        IsVisible = int.Parse(inputs[20]); // 0 if it isn't
        ItemsOwned = int.Parse(inputs[21]); // useful from wood1
    }

    public bool IsMyHero(int myTeamId)
    {
        return HeroType != "-" && Team == myTeamId;
    }

    public int HealthLost()
    {
        return (Health / MaxHealth < 0.5) ? MaxHealth - Health : 0;
    }
}

class Hero
{
    public string Name { get; set; }
    public Entity Current { get; set; }
    public Entity Previous { get; set; }
    public List<ShopItem> Items { get; set; }
    public List<Spell> Spells { get; set; }
    public int DefaultY { get; set; }

    public Hero(string Name, int Y)
    {
        this.Name = Name;
        this.DefaultY = Y;
        Items = new List<ShopItem>();

        if (Name == "IRONMAN") Spells = GetIronmanSpells();
        else if (Name == "DOCTOR_STRANGE") Spells = GetDoctorStrangeSpells();
    }

    private static List<Spell> GetDoctorStrangeSpells()
    {
        return new List<Spell>{
                new Spell {
                    Name = "AOEHEAL {0} {1}",
                    ManaCost = 50,
                    CoolDownSlot = "1",
                    Range = 250,
                    Type = "Healing",
                    Target = "AOE"
                },
                new Spell {
                    Name = "SHIELD {0}",
                    ManaCost = 40,
                    CoolDownSlot = "2",
                    Range = 500,
                    Type = "Healing2",
                    Target = "Single"
                },
                new Spell {
                    Name = "PULL {0}",
                    ManaCost = 40,
                    CoolDownSlot = "3",
                    Range = 400,
                    Type = "Offensive",
                    Target = "Single"
                }
            };
    }

    private static List<Spell> GetIronmanSpells()
    {
        return new List<Spell>{
                new Spell {
                    Name = "BLINK {0} {1}",
                    ManaCost = 16,
                    CoolDownSlot = "1",
                    Range = 200,
                    Type = "Defensive",
                    Target = "AOE"
                },
                new Spell {
                    Name = "FIREBALL {0} {1}",
                    ManaCost = 60,
                    CoolDownSlot = "2",
                    Range = 900,
                    Type = "Offensive",
                    Target = "AOE"
                },
                new Spell {
                    Name = "BURNING {0} {1}",
                    ManaCost = 50,
                    CoolDownSlot = "3",
                    Range = 250,
                    Type = "Offensive",
                    Target = "AOE"
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
 */
