using ProtoBuf;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using XLib.XEffects;
using XLib.XLeveling;

namespace XSkills
{
    public static class PlayerExtensions
    {
        public static float OnAcquireTransitionSpeed(this IPlayer player, EnumTransitionType transType, ItemStack stack, float mulByConfig)
        {
            if (transType != EnumTransitionType.Perish) return 1.0f;
            return player.Entity.Stats.GetBlended("perishMult");
        }
    }

    public class Cooking : XSkill
    {
        //ability ids
        public int CanteenCookId { get; private set; }
        public int FastFoodId { get; private set; }
        public int WellDoneId { get; private set; }
        public int PreserverId { get; private set; }
        public int DilutionId { get; private set; }
        public int DesalinateId { get; private set; }
        public int SaltyBackpackId { get; private set; }
        public int GourmetId { get; private set; }
        public int HappyMealId { get; private set; }
        public int JuicerId { get; private set; }
        public int EggTimerId { get; private set; }

        protected Dictionary<CookingRecipeStack, List<CookingRecipeStack>> resolvedRecipeStacks = new();
        public Cooking(ICoreAPI api) : base("cooking", "xskills:skill-cooking", "xskills:group-processing")
        {
            (XLeveling.Instance(api))?.RegisterSkill(this);
            this.Config = new CookingSkillConfig();

            // cook more servings at once
            // 0: value
            CanteenCookId = this.AddAbility(new Ability(
                "canteencook",
                "xskills:ability-canteencook",
                "xskills:abilitydesc-canteencook",
                1, 3, new int[] { 34, 67, 100 }));

            // cook faster
            // 0: base value
            // 1: value per level
            // 2: max value
            FastFoodId = this.AddAbility(new Ability(
                "fastfood",
                "xskills:ability-fastfood",
                "xskills:abilitydesc-fastfood",
                1, 3, new int[] { 10, 1, 20, 20, 2, 40, 20, 2, 60 }));

            //increases the shelf life of your cooked servings
            // 0: base value
            // 1: value per level
            // 2: max value
            // 3: increased cooking time
            WellDoneId = this.AddAbility(new Ability(
                "welldone",
                "xskills:ability-welldone",
                "xskills:abilitydesc-welldone",
                1, 3, new int[] { 5, 1, 15, 20, 10, 2, 30, 20, 10, 2, 50, 20 }));

            //increases the number of servings you gain when cooking
            // 0: base value
            // 1: value per level
            // 2: max value
            DilutionId = this.AddAbility(new Ability(
                "dilution",
                "xskills:ability-dilution",
                "xskills:abilitydesc-dilution",
                3, 3, new int[] { 10, 1, 20, 20, 1, 30, 20, 1, 40 }));

            // you can cook water into salt
            // 0: primary result
            // 1: secondary result
            DesalinateId = this.AddAbility(new Ability(
                "desalinate",
                "xskills:ability-desalinate",
                "xskills:abilitydesc-desalinate",
                3, 3, new int[] { 1, 1, 2, 1, 4, 2 }));

            // ingredients in backpack perish slower
            // 0: water needed per salt
            SaltyBackpackId = this.AddAbility(new Ability(
                "saltybackpack",
                "xskills:ability-saltybackpack",
                "xskills:abilitydesc-saltybackpack",
                3, 3, new int[] { 75, 66, 50 }));

            // enables quality for food
            // 0: base value
            // 1: max value
            GourmetId = this.AddAbility(new Ability(
                "gourmet",
                "xskills:ability-gourmet",
                "xskills:abilitydesc-gourmet",
                3, 2, new int[] { 1, 5, 2, 10 }));

            // profession
            // 0: base value
            SpecialisationID = this.AddAbility(new Ability(
                "chef",
                "xskills:ability-chef",
                "xskills:abilitydesc-chef",
                5, 1, new int[] { 40 }));

            // chance to add a random ingredient
            // 0: base value
            // 1: value per level
            // 2: max value
            HappyMealId = this.AddAbility(new Ability(
                "happymeal",
                "xskills:ability-happymeal",
                "xskills:abilitydesc-happymeal",
                5, 3, new int[] { 10, 1, 20, 20, 2, 40, 20, 2, 60 }));

            // Increases the amount of juice for every fruit
            // 0: increased amount
            JuicerId = this.AddAbility(new Ability(
                "juicer",
                "xskills:ability-juicer",
                "xskills:abilitydesc-juicer",
                6, 2, new int[] { 33, 60 }));

            // sends a message to the player when cooking a meal was finished
            EggTimerId = this.AddAbility(new Ability(
                "eggtimer",
                "xskills:ability-eggtimer",
                "xskills:abilitydesc-eggtimer",
                8));

            this[SaltyBackpackId].OnPlayerAbilityTierChanged += OnSaltyBackpack;

            this.ExperienceEquation = QuadraticEquation;
            this.ExpBase = 40;
            this.ExpMult = 10.0f;
            this.ExpEquationValue = 0.8f;

            CookingRecipe.NamingRegistry["lime"] = new XskillsCookingRecipeNames();
            CookingRecipe.NamingRegistry["salt"] = new XskillsCookingRecipeNames();
        }

        public static void ApplyQuality(float quality, float eaten, float temperature, EnumFoodCategory food0, EnumFoodCategory food1, EntityAgent byEntity)
        {
            if (quality <= 0.0f || float.IsNaN(quality) || byEntity.Api.Side == EnumAppSide.Client) return;
            if (eaten <= 0.0f) return;
            float duration = eaten * 600.0f;

            string effectName;
            if (food0 == EnumFoodCategory.Fruit)
            {
                effectName = "saturated-hot";
            }
            else if (food0 == EnumFoodCategory.Vegetable)
            {
                effectName = "saturated-miningSpeed";
            }
            else if (food0 == EnumFoodCategory.Protein)
            {
                effectName = "saturated-health";
            }
            else if (food0 == EnumFoodCategory.Grain)
            {
                effectName = "saturated-hungerrate";
            }
            else if (food0 == EnumFoodCategory.Dairy)
            {
                effectName = "saturated-expMult";
            }
            else return;

            XEffectsSystem effectSystem = byEntity?.Api.ModLoader.GetModSystem<XEffectsSystem>();
            if (effectSystem == null) return;
            Effect effect = effectSystem.CreateEffect(effectName);
            if (effect != null)
            {
                effect.Duration *= duration;
                effect.Update(effect.Intensity * quality);
                byEntity.AddEffect(effect);
            }

            if (temperature >= 50.0f)
            {
                effectName = "saturated-heated";
                effect = effectSystem.CreateEffect(effectName);
                if (effect != null)
                {
                    effect.Duration *= duration;
                    effect.Update(effect.Intensity * quality);
                    byEntity.AddEffect(effect);
                }
            }
        }

        public ItemStack[] ContentStacks(ItemStack itemStack, IWorldAccessor world)
        {
            ItemStack[] contentStacks;
            IBlockMealContainer mealContainer = (itemStack.Collectible as IBlockMealContainer);
            ItemStack liquidStack = (itemStack.Collectible as BlockLiquidContainerBase)?.GetContent(itemStack);
            if (mealContainer != null)
            {
                contentStacks = mealContainer.GetContents(world, itemStack);
            }
            else if (liquidStack != null)
            {
                contentStacks = new ItemStack[] { liquidStack };
            }
            else
            {
                contentStacks = new ItemStack[] { itemStack };
            }
            return contentStacks;
        }

        public float IngredientDiversity(ItemStack itemStack, ItemStack[] contentStacks, IWorldAccessor world, out int ingredientCount)
        {
            ingredientCount = 0;
            int substract = 1;
            if (itemStack == null) return 0.0f;
            if (contentStacks == null) contentStacks = ContentStacks(itemStack, world);

            Dictionary<CollectibleObject, int> usedIngredients = new Dictionary<CollectibleObject, int>();
            foreach (ItemStack ingridient in contentStacks)
            {
                if (ingridient == null) continue;
                ingredientCount++;

                if (!usedIngredients.TryGetValue(ingridient.Collectible, out int value)) value = 0;
                usedIngredients[ingridient.Collectible] = value + 1;
            }

            //expanded foods
            string[] madeWith = (itemStack.Attributes["madeWith"] as StringArrayAttribute)?.value;
            Dictionary<string, int> madeWithIngredients = new Dictionary<string, int>();
            if (madeWith?.Length > 0)
            {
                substract++;
                ingredientCount--;
                foreach (string ingredient in madeWith)
                {
                    if (ingredient == null) continue;
                    ingredientCount++;

                    if (!madeWithIngredients.TryGetValue(ingredient, out int value)) value = 0;
                    madeWithIngredients[ingredient] = value + 1;
                }
            }
            return 1.0f + (usedIngredients.Count + madeWithIngredients.Count - substract) * 0.1f;
        }

        public float BakeRange(ItemStack outputStack, ItemStack sourceStack, out bool firstStage)
        {
            BakingProperties bakingProperties = BakingProperties.ReadFrom(outputStack);
            BakingProperties bakingProperties2 = sourceStack != null ? BakingProperties.ReadFrom(sourceStack) : null;
            float bakeRange = 1.0f;
            firstStage = true;
            if (bakingProperties2 == null) return bakeRange;
            if (bakingProperties == null)
            {
                bakeRange = GameMath.Clamp(1.0f - bakingProperties2.LevelFrom, 0.0f, 1.0f);
            }
            else
            {
                bakeRange = Math.Min(bakingProperties.LevelFrom - bakingProperties2.LevelFrom, 1.0f);
            }
            firstStage = bakingProperties2.LevelFrom <= 0.0f;
            return Math.Abs(bakeRange);
        }

        public bool FinishedCooking(ItemSlot outputSlot)
        {
            if ((outputSlot.Inventory as InventorySmelting)?[1].Empty ?? false) return true;
            foreach (ItemSlot slot in outputSlot.Inventory)
            {
                if (slot.Empty) continue;

                BakingProperties bakingProperties = BakingProperties.ReadFrom(slot.Itemstack);
                if (bakingProperties != null)
                {
                    if (slot == outputSlot)
                    {
                        if (bakingProperties.ResultCode?.Contains("charred") ?? true)
                        {
                            return true;
                        }
                        else return false;
                    }
                    else continue;
                }

                if (slot == outputSlot) continue;
                if (slot.Itemstack.Collectible.CombustibleProps?.BurnDuration > 0.0f) continue;
                return false;
            }
            return true;
        }

        public void FreshnessAndQuality(ItemStack[] sourceStacks, out float freshness, out float quality)
        {
            freshness = 1.0f;
            quality = 0.0f;
            int ingredientCount = 0;

            foreach (ItemStack stack in sourceStacks)
            {
                ITreeAttribute attr = (stack?.Attributes as TreeAttribute)?.GetTreeAttribute("transitionstate");
                if (attr != null)
                {
                    FloatArrayAttribute freshHoursAttribute = attr["freshHours"] as FloatArrayAttribute;
                    FloatArrayAttribute transitionedHoursAttribute = attr["transitionedHours"] as FloatArrayAttribute;
                    if (freshHoursAttribute == null || transitionedHoursAttribute == null) continue;

                    for (int ii = 0; ii < freshHoursAttribute.value.Length; ++ii)
                    {
                        if (freshHoursAttribute.value[ii] != 0.0f)
                        {
                            freshness *= Math.Clamp(1.0f - transitionedHoursAttribute.value[ii] / freshHoursAttribute.value[ii], 0.0f, 1.0f);
                            break;
                        }
                    }
                }

                int count = (stack.Attributes["madeWith"] as StringArrayAttribute)?.value.Length ?? 1;
                quality += stack.Attributes.GetFloat("quality") * count;
                ingredientCount += count;
            }
            quality /= ingredientCount;
        }

        public void ApplyAbilities(ItemSlot outputSlot, IPlayer player, float oldQuality, float cookedAmount = 1.0f, ItemStack[] sourceStacks = null, float expMult = 1.0f)
        {
            ItemStack outputStack = outputSlot?.Itemstack;
            if (outputStack == null || player == null) return;
            ItemStack[] contentStacks;
            ItemStack sourceStack = sourceStacks?.Length == 1 ? sourceStacks[0] : null;

            PlayerSkill skill = player.Entity.GetBehavior<PlayerSkillSet>()?[this.Id];
            if (skill == null) return;
            IWorldAccessor world = player.Entity?.World;
            if (world == null) return;

            contentStacks = ContentStacks(outputStack, world);
            if (contentStacks == null || contentStacks.Length < 1) return;

            float bakeRange = BakeRange(outputStack, sourceStack, out bool firstStage);
            if (bakeRange < 0.99f) bakeRange *= 1.5f;

            float satiety = outputStack.Collectible.NutritionProps?.Satiety ?? 0.0f;
            float servings = (float)outputStack.Attributes.GetDecimal("quantityServings", cookedAmount);
            bool charred = (sourceStack?.Collectible.NutritionProps?.Satiety ?? 0.0f) > satiety || outputStack.Collectible.Code.Path.Contains("charred");
            float ingredientDiversity = IngredientDiversity(outputStack, contentStacks, world, out int ingredientCount);
            bool expandedFood = outputStack.Attributes.HasAttribute("madeWith");
            IBlockMealContainer mealContainer = (outputStack.Collectible as IBlockMealContainer);
            BlockLiquidContainerBase liquidContainer = (outputStack.Collectible as BlockLiquidContainerBase);

            //experience
            float exp = expMult * (Config as CookingSkillConfig).expBase;
            if (ingredientCount == 1)
            {
                exp *= satiety * servings * bakeRange;
            }
            else
            {
                exp *= 225.0f * ingredientCount * ingredientDiversity * servings * bakeRange;
            }

            if (!charred)
            {
                if ((!expandedFood || satiety > 0.0f))
                {
                    skill.AddExperience(exp);
                }
            }
            else if (firstStage)
            {
                skill.AddExperience(exp * 0.5f);
            }

            //eggtimer
            PlayerAbility playerAbility = skill[this.EggTimerId];
            if (playerAbility?.Tier > 0)
            {
                BlockPos pos = outputSlot.Inventory.Pos;
                Block block = pos != null ? world.BulkBlockAccessor.GetBlock(pos) : null;

                if (block != null && FinishedCooking(outputSlot))
                {
                    double now = world.Calendar.TotalHours;
                    double lastMsg = player.Entity.Attributes.GetDouble("xskillsCookingMsg");

                    if (now > lastMsg + 0.333)
                    {
                        player.Entity.Attributes.SetDouble("xskillsCookingMsg", now);
                        world.PlaySoundFor(new AssetLocation("sounds/tutorialstepsuccess.ogg"), player);

                        string msg = Lang.Get("xskills:cooking-finished", block.GetPlacedBlockName(world, pos) + " (" + pos.X + ", " + pos.X + pos.Y + ", " + pos.Z + ")");
                        (player as IServerPlayer)?.SendMessage(0, msg, EnumChatType.Notification);
                    }
                }
            }

            //dilution
            playerAbility = skill[this.DilutionId];
            float scaledCooked = servings;
            int totalCooked = (int)cookedAmount;
            if (playerAbility?.Tier > 0 && firstStage && !outputStack.Collectible.Code.Path.Equals("glueportion-pitch-hot"))
            {
                scaledCooked = servings * (1.0f + playerAbility.SkillDependentFValue());
                if (liquidContainer != null)
                {
                    float mult = 1.0f + playerAbility.SkillDependentFValue();
                    foreach (ItemStack stack in contentStacks)
                    {
                        if (stack.Collectible.NutritionProps?.Satiety > 0.0f)
                            stack.StackSize = (int)(stack.StackSize * mult);
                    }
                }
                else if (mealContainer == null || mealContainer is BlockPie)
                {
                    float rel = scaledCooked - (int)scaledCooked;
                    totalCooked = (int)scaledCooked + (world.Rand.NextDouble() < rel ? 1 : 0);
                    if (outputStack.StackSize > cookedAmount) outputStack.StackSize += totalCooked - (int)(cookedAmount + 0.25f);
                    else outputStack.StackSize = totalCooked;
                }
                else
                {
                    mealContainer.SetQuantityServings(world, outputStack, scaledCooked);
                }
            }

            //desalinate
            playerAbility = skill[this.DesalinateId];
            if (playerAbility.Tier > 0 && (
                outputStack.Collectible.Code.Path.Equals("salt") ||
                outputStack.Collectible.Code.Path.Equals("lime")))
            {
                ItemSlot[] slots = (outputSlot.Inventory as InventorySmelting)?.CookingSlots;
                ItemSlot slot = (slots?.Length ?? 0) > 1 ? slots[1] : null;

                int size0 = outputStack.StackSize * playerAbility.Value(0);
                int size1 = outputStack.StackSize * playerAbility.Value(1);
                if (slot != null && slot.Itemstack == null)
                {
                    string itemName = outputStack.Collectible.Code.Path.Equals("salt") ? "game:lime" : "game:salt";
                    outputStack.StackSize = size0;
                    if (outputStack.StackSize == 0)
                    {
                        outputStack = null;
                        outputSlot.Itemstack = null;
                    }

                    Item itemLime = world.GetItem(new AssetLocation(itemName));
                    ItemStack stack = itemLime != null && size1 > 0 ? new ItemStack(itemLime, size1) : null;
                    slot.Itemstack = stack;
                    slot.MarkDirty();
                    outputSlot.MarkDirty();
                }
            }
            if (outputStack == null) return;

            if (mealContainer != null)
            {
                //Happy meal
                playerAbility = skill[this.HappyMealId];
                if (playerAbility?.SkillDependentFValue() >= world.Rand.NextDouble() && mealContainer is BlockCookedContainer)
                {
                    ItemStack[] newStacks = new ItemStack[contentStacks.Length + 1];
                    int ii = 0;
                    int size = 0;
                    ITreeAttribute attr = null;
                    for (; ii < contentStacks.Length; ii++)
                    {
                        newStacks[ii] = contentStacks[ii];
                        size += contentStacks[ii]?.StackSize ?? 0;

                        if (attr != null) continue;
                        attr = (contentStacks[ii]?.Attributes as TreeAttribute)?.GetTreeAttribute("transitionstate");
                        if (attr?.HasAttribute("freshHours") ?? false)
                        {
                            attr = attr.Clone();
                        }
                        else attr = null;
                    }
                    size /= contentStacks.Length;

                    CookingRecipe recipe = (mealContainer as BlockCookedContainer)?.GetCookingRecipe(world, outputStack) ?? (mealContainer as BlockMeal)?.GetCookingRecipe(world, outputStack);
                    if (recipe != null)
                    {
                        ItemStack stack = GetMissingIngredient(contentStacks, recipe, world, true);
                        stack.StackSize = size;
                        if (attr != null) stack.Attributes["transitionstate"] = attr;
                        if (stack != null)
                        {
                            newStacks[ii] = stack;
                            mealContainer.SetContents(recipe.Code, outputStack, newStacks, mealContainer.GetQuantityServings(world, outputStack));
                        }
                    }
                }
            }

            //well done
            playerAbility = skill[this.WellDoneId];
            foreach (ItemStack stack in contentStacks)
            {
                ITreeAttribute attr = (stack?.Attributes as TreeAttribute)?.GetTreeAttribute("transitionstate");
                if (attr != null)
                {
                    FloatArrayAttribute freshHoursAttribute = attr["freshHours"] as FloatArrayAttribute;
                    FloatArrayAttribute transitionedHoursAttribute = attr["transitionedHours"] as FloatArrayAttribute;
                    if (freshHoursAttribute == null || transitionedHoursAttribute == null) continue;

                    for (int ii = 0; ii < freshHoursAttribute.value.Length; ++ii)
                    {
                        if (freshHoursAttribute.value[ii] != 0.0f)
                        {
                            if (mealContainer == null)
                            {
                                TransitionableProperties[] transProps = outputStack.Collectible.TransitionableProps;
                                if (transProps != null)
                                {
                                    freshHoursAttribute.value[ii] = transProps[ii].FreshHours.avg * (1.0f + playerAbility.SkillDependentFValue());
                                }
                            }
                            else
                            {
                                freshHoursAttribute.value[ii] = freshHoursAttribute.value[ii] * (1.0f + playerAbility.SkillDependentFValue());
                            }
                        }
                    }
                }
            }
            FreshnessAndQuality(sourceStacks ?? contentStacks, out float freshness, out float sourceQuality);
            if (float.IsNaN(sourceQuality)) sourceQuality = 0.0f;

            //gourmet
            playerAbility = skill[this.GourmetId];
            if (playerAbility?.Tier > 0)
            {
                float quality;
                if ((sourceStacks != null ? sourceStacks.Length : contentStacks.Length) == 1 && sourceQuality > 0.0f)
                {
                    quality = sourceQuality * (charred ? 0.2f : 1.1f);
                }
                else
                {
                    float toCalc = playerAbility.Value(1) - sourceQuality;
                    quality = Math.Min(skill.Level, 25) * 0.1f + 2.0f * freshness + ingredientCount * 0.2f + ingredientDiversity;
                    quality *= 0.3125f * playerAbility.Value(0);
                    quality = Math.Min(quality + (float)world.Rand.NextDouble() * quality, playerAbility.Value(1));
                    quality = quality / playerAbility.Value(1) * toCalc;
                    quality += (sourceQuality * 1.1f);
                }
                if (liquidContainer == null)
                {
                    outputStack.Attributes.SetFloat("quality", (quality * totalCooked + oldQuality * (outputStack.StackSize - totalCooked)) / outputStack.StackSize);
                }
                else
                {
                    foreach (ItemStack stack in contentStacks)
                    {
                        stack.Attributes.SetFloat("quality", (quality));
                    }
                }
            }
            liquidContainer?.SetContents(outputStack, contentStacks);
        }

        protected CookingRecipeStack GetResolvedIngredient(IWorldAccessor world, CookingRecipeStack recipeStack)
        {
            if (!recipeStack.Code.Path.Contains('*')) return recipeStack;
            resolvedRecipeStacks.TryGetValue(recipeStack, out List<CookingRecipeStack> stacks);

            if (stacks is null)
            {
                stacks = new List<CookingRecipeStack>();
                if (recipeStack.Type == EnumItemClass.Item)
                {
                    foreach (Item item in world.Items)
                    {
                        if (item.WildCardMatch(recipeStack.Code))
                        {
                            CookingRecipeStack newRecipeStack = recipeStack.Clone();
                            newRecipeStack.Code = item.Code;
                            newRecipeStack.ResolvedItemstack = new ItemStack(item);
                            stacks.Add(newRecipeStack);
                        }
                    }
                }
                if (recipeStack.Type == EnumItemClass.Block || recipeStack.Code.Path.Contains("mushroom"))
                {
                    foreach (Block block in world.Blocks)
                    {
                        if (block.WildCardMatch(recipeStack.Code))
                        {
                            CookingRecipeStack newRecipeStack = recipeStack.Clone();
                            newRecipeStack.Code = block.Code;
                            newRecipeStack.ResolvedItemstack = new ItemStack(block);
                            stacks.Add(newRecipeStack);
                        }
                    }
                }
                resolvedRecipeStacks.Add(recipeStack, stacks);
            }
            if (stacks.Count == 0) return recipeStack;
            return stacks[world.Rand.Next(stacks.Count - 1)];
        }

        public ItemStack GetMissingIngredient(ItemStack[] inputStacks, CookingRecipe recipe, IWorldAccessor world, bool allowBad)
        {
            List<ItemStack> inputStacksList = new List<ItemStack>(inputStacks);
            List<CookingRecipeIngredient> ingredientList = new List<CookingRecipeIngredient>(recipe.Ingredients);

            int[] quantities = new int[ingredientList.Count];

            while (inputStacksList.Count > 0)
            {
                ItemStack inputStack = inputStacksList[0];
                inputStacksList.RemoveAt(0);
                if (inputStack == null) continue;

                for (int ii = 0; ii < ingredientList.Count; ii++)
                {
                    CookingRecipeIngredient ingred = ingredientList[ii];
                    if (ingred.Matches(inputStack))
                    {
                        quantities[ii]++;
                        if (quantities[ii] >= ingred.MaxQuantity)
                        {
                            ingredientList.RemoveAt(ii);
                        }
                        break;
                    }
                }
            }

            int tries = 0;
            ItemStack stack = null;
            while (tries < 5 && stack == null && ingredientList.Count > 0)
            {
                tries++;
                CookingRecipeIngredient ingred = ingredientList[world.Rand.Next(ingredientList.Count - 1)];
                if (ingred.ValidStacks.Length == 0)
                {
                    ingredientList.Remove(ingred);
                    continue;
                }

                int tries2 = 0;
                while (tries2 < 5 && stack == null)
                {
                    tries2++;
                    CookingRecipeStack recipeStack = ingred.ValidStacks[world.Rand.Next(ingred.ValidStacks.Length - 1)];
                    recipeStack = GetResolvedIngredient(world, recipeStack);
                    if (recipeStack?.ResolvedItemstack == null) continue;

                    if (allowBad || recipeStack.ResolvedItemstack.Collectible.NutritionProps.Health >= 0)
                    {
                        stack = recipeStack.ResolvedItemstack.Clone();
                    }
                }
            }
            if (stack != null) stack.StackSize = 1;
            return stack;
        }

        public void OnSaltyBackpack(PlayerAbility playerAbility, int oldTier)
        {
            IPlayer player = playerAbility.PlayerSkill.PlayerSkillSet.Player;
            player.Entity.Stats.Set("perishMult", "ability", - 1.0f + playerAbility.FValue(0));

            InventoryBase backPackInv = player.InventoryManager.GetOwnInventory(GlobalConstants.backpackInvClassName) as InventoryBase;
            InventoryBase hotBarInv = player.InventoryManager.GetOwnInventory(GlobalConstants.hotBarInvClassName) as InventoryBase;

            if (backPackInv != null)
            {
                if (playerAbility.Tier == 0 && oldTier > 0) backPackInv.OnAcquireTransitionSpeed -= player.OnAcquireTransitionSpeed;
                if (playerAbility.Tier > 0 && oldTier == 0) backPackInv.OnAcquireTransitionSpeed += player.OnAcquireTransitionSpeed;
            }
            if (hotBarInv != null)
            {
                if (playerAbility.Tier == 0 && oldTier > 0) hotBarInv.OnAcquireTransitionSpeed -= player.OnAcquireTransitionSpeed;
                if (playerAbility.Tier > 0 && oldTier == 0) hotBarInv.OnAcquireTransitionSpeed += player.OnAcquireTransitionSpeed;
            }
        }
    }//!class Cooking

    [ProtoContract]
    public class CookingSkillConfig : CustomSkillConfig
    {
        public override Dictionary<string, string> Attributes
        {
            get
            {
                CultureInfo provider = new CultureInfo("en-US");

                Dictionary<string, string> result = new Dictionary<string, string>();
                result.Add("expBase", this.expBase.ToString(provider));
                result.Add("fruitPressExpPerLitre", this.fruitPressExpPerLitre.ToString(provider));
                return result;
            }
            set
            {
                string str;
                NumberStyles styles = NumberStyles.Any;
                CultureInfo provider = new CultureInfo("en-US");

                value.TryGetValue("expBase", out str);
                if (str != null) float.TryParse(str, styles, provider, out this.expBase);
                value.TryGetValue("fruitPressExpPerLitre", out str);
                if (str != null) float.TryParse(str, styles, provider, out this.fruitPressExpPerLitre);
            }
        }

        [ProtoMember(1)]
        [DefaultValue(0.0004f)]
        public float expBase = 0.0004f;

        [ProtoMember(2)]
        [DefaultValue(0.05f)]
        public float fruitPressExpPerLitre = 0.05f;
    }

    public class XskillsCookingRecipeNames : ICookingRecipeNamingHelper
    {
        public string GetNameForIngredients(IWorldAccessor worldForResolve, string recipeCode, ItemStack[] stacks)
        {
            if (recipeCode == null) return Lang.Get("game:unknown");
            CookingRecipe recipe = worldForResolve.Api.GetCookingRecipe(recipeCode);
            if (recipe == null) return Lang.Get("game:unknown");
            ItemStack resultStack = recipe.CooksInto?.ResolvedItemstack;
            if (resultStack == null) return Lang.Get("game:unknown");

            switch (recipeCode)
            {
                case "lime":
                    return resultStack.Collectible.GetHeldItemName(resultStack) + "\n" + Lang.Get("game:item-handbooktext-lime");
                case "salt":
                    return resultStack.Collectible.GetHeldItemName(resultStack) + "\n" + Lang.Get("game:item-handbooktext-salt");
                default:
                    return "";
            }
        }
    }
}//!namespace XSkills
