using System.Collections.Generic;
using System.Linq;

using CitizenFX.Core;

using MenuAPI;

using vMenuClient.data;

using static CitizenFX.Core.Native.API;
using static vMenuClient.CommonFunctions;
using static vMenuShared.PermissionsManager;

namespace vMenuClient.menus
{
    public class WeaponOptions
    {
        // Variables
        private Menu menu;

        public bool UnlimitedAmmo { get; private set; } = UserDefaults.WeaponsUnlimitedAmmo;
        public bool NoReload { get; private set; } = UserDefaults.WeaponsNoReload;
        public bool AutoEquipChute { get; private set; } = UserDefaults.AutoEquipChute;
        public bool UnlimitedParachutes { get; private set; } = UserDefaults.WeaponsUnlimitedParachutes;

        public static Dictionary<string, uint> AddonWeapons = new();

        private Dictionary<Menu, ValidWeapon> weaponInfo;
        private Dictionary<MenuItem, string> weaponComponents;

        #region Create Menu
        /// <summary>
        /// Creates the menu.
        /// </summary>
        private void CreateMenu()
        {
            // Setup weapon dictionaries.
            weaponInfo = new Dictionary<Menu, ValidWeapon>();
            weaponComponents = new Dictionary<MenuItem, string>();

            #region create main weapon options menu and add items
            // Create the menu.
            menu = new Menu(Game.Player.Name, "武器功能选项");

            var getAllWeapons = new MenuItem("获取全部武器", "直接获得原生全部武器装备.");
            var removeAllWeapons = new MenuItem("移除全部武器", "移除库存中原生全部武器装备.");
            var unlimitedAmmo = new MenuCheckboxItem("无限弹药", "弹药供应无限制.", UnlimitedAmmo);
            var noReload = new MenuCheckboxItem("无需装弹", "无需武器装弹功能.", NoReload);
            var setAmmo = new MenuItem("设置所有弹药数", "设置所有武器的弹药量.");
            var refillMaxAmmo = new MenuItem("填满所有弹药", "为所有武器提供最大弹药.");
            var spawnByName = new MenuItem("按代码生成武器", "输入武器模型名称以生成.");


            // Add items based on permissions
            if (IsAllowed(Permission.WPGetAll))
            {
                menu.AddMenuItem(getAllWeapons);
            }
            if (IsAllowed(Permission.WPRemoveAll))
            {
                menu.AddMenuItem(removeAllWeapons);
            }
            if (IsAllowed(Permission.WPUnlimitedAmmo))
            {
                menu.AddMenuItem(unlimitedAmmo);
            }
            if (IsAllowed(Permission.WPNoReload))
            {
                menu.AddMenuItem(noReload);
            }
            if (IsAllowed(Permission.WPSetAllAmmo))
            {
                menu.AddMenuItem(setAmmo);
                menu.AddMenuItem(refillMaxAmmo);
            }
            if (IsAllowed(Permission.WPSpawnByName))
            {
                menu.AddMenuItem(spawnByName);
            }
            #endregion

            #region addonweapons submenu
            var addonWeaponsBtn = new MenuItem("添加式武器", "装备/移除本服务器提供的添加式武器.");
            var addonWeaponsMenu = new Menu("添加式武器", "装备/移除添加式武器");
            menu.AddMenuItem(addonWeaponsBtn);

            #region manage creating and accessing addon weapons menu
            if (IsAllowed(Permission.WPSpawn) && AddonWeapons != null && AddonWeapons.Count > 0)
            {
                MenuController.BindMenuItem(menu, addonWeaponsMenu, addonWeaponsBtn);
                foreach (var weapon in AddonWeapons)
                {
                    var name = weapon.Key.ToString();
                    var model = weapon.Value;
                    var item = new MenuItem(name, $"点击以将此武器（{name}）添加到/从你的库存中移除.");
                    addonWeaponsMenu.AddMenuItem(item);
                    if (!IsWeaponValid(model))
                    {
                        item.Enabled = false;
                        item.LeftIcon = MenuItem.Icon.LOCK;
                        item.Description = "此模型不可用.请联系服务器管理员确认模型是否正确流式传输.";
                    }
                }
                addonWeaponsMenu.OnItemSelect += (sender, item, index) =>
                {
                    var weapon = AddonWeapons.ElementAt(index);
                    if (HasPedGotWeapon(Game.PlayerPed.Handle, weapon.Value, false))
                    {
                        RemoveWeaponFromPed(Game.PlayerPed.Handle, weapon.Value);
                    }
                    else
                    {
                        var maxAmmo = 200;
                        GetMaxAmmo(Game.PlayerPed.Handle, weapon.Value, ref maxAmmo);
                        GiveWeaponToPed(Game.PlayerPed.Handle, weapon.Value, maxAmmo, false, true);
                    }
                };
                addonWeaponsBtn.Label = "→→→";
            }
            else
            {
                addonWeaponsBtn.LeftIcon = MenuItem.Icon.LOCK;
                addonWeaponsBtn.Enabled = false;
                addonWeaponsBtn.Description = "此选项在当前服务器上不可用,因为你没有权限使用它,或它未正确设置.";
            }
            #endregion
            addonWeaponsMenu.RefreshIndex();
            #endregion

            #region parachute options menu

            if (IsAllowed(Permission.WPParachute))
            {
                // main parachute options menu setup
                var parachuteMenu = new Menu("降落伞选项", "降落伞选项");
                var parachuteBtn = new MenuItem("降落伞选项", "在这里可以更改所有与降落伞相关的选项.") { Label = "→→→" };

                MenuController.AddSubmenu(menu, parachuteMenu);
                menu.AddMenuItem(parachuteBtn);
                MenuController.BindMenuItem(menu, parachuteMenu, parachuteBtn);

                var chutes = new List<string>()
                {
                    GetLabelText("PM_TINT0"),
                    GetLabelText("PM_TINT1"),
                    GetLabelText("PM_TINT2"),
                    GetLabelText("PM_TINT3"),
                    GetLabelText("PM_TINT4"),
                    GetLabelText("PM_TINT5"),
                    GetLabelText("PM_TINT6"),
                    GetLabelText("PM_TINT7"),

                    // broken in FiveM for some weird reason:
                    GetLabelText("PS_CAN_0"),
                    GetLabelText("PS_CAN_1"),
                    GetLabelText("PS_CAN_2"),
                    GetLabelText("PS_CAN_3"),
                    GetLabelText("PS_CAN_4"),
                    GetLabelText("PS_CAN_5")
                };
                var chuteDescriptions = new List<string>()
                {
                    GetLabelText("PD_TINT0"),
                    GetLabelText("PD_TINT1"),
                    GetLabelText("PD_TINT2"),
                    GetLabelText("PD_TINT3"),
                    GetLabelText("PD_TINT4"),
                    GetLabelText("PD_TINT5"),
                    GetLabelText("PD_TINT6"),
                    GetLabelText("PD_TINT7"),

                    // broken in FiveM for some weird reason:
                    GetLabelText("PSD_CAN_0") + " ~r~由于某些原因,这一条在 FiveM 中似乎不起作用.",
                    GetLabelText("PSD_CAN_1") + " ~r~由于某些原因,这一条在 FiveM 中似乎不起作用.",
                    GetLabelText("PSD_CAN_2") + " ~r~由于某些原因,这一条在 FiveM 中似乎不起作用.",
                    GetLabelText("PSD_CAN_3") + " ~r~由于某些原因,这一条在 FiveM 中似乎不起作用.",
                    GetLabelText("PSD_CAN_4") + " ~r~由于某些原因,这一条在 FiveM 中似乎不起作用.",
                    GetLabelText("PSD_CAN_5") + " ~r~由于某些原因,这一条在 FiveM 中似乎不起作用."
                };

                var togglePrimary = new MenuItem("切换主降落伞", "装备或移除主降落伞");
                var toggleReserve = new MenuItem("启用备用降落伞", "启用备用降落伞.只有在先启用主降落伞后才有效.一旦激活.备用降落伞无法从玩家身上移除.");
                var primaryChutes = new MenuListItem("主降落伞样式", chutes, 0, $"主降落伞: {chuteDescriptions[0]}");
                var secondaryChutes = new MenuListItem("备用降落伞样式", chutes, 0, $"备用降落伞: {chuteDescriptions[0]}");
                var unlimitedParachutes = new MenuCheckboxItem("无限降落伞", "启用无限降落伞和备用降落伞.", UnlimitedParachutes);
                var autoEquipParachutes = new MenuCheckboxItem("自动装备降落伞", "进入飞机/直升机时自动装备降落伞和备用降落伞.", AutoEquipChute);

                // smoke color list
                var smokeColorsList = new List<string>()
                {
                    GetLabelText("PM_TINT8"), // no smoke
                    GetLabelText("PM_TINT9"), // red
                    GetLabelText("PM_TINT10"), // orange
                    GetLabelText("PM_TINT11"), // yellow
                    GetLabelText("PM_TINT12"), // blue
                    GetLabelText("PM_TINT13"), // black
                };
                var colors = new List<int[]>()
                {
                    new int[3] { 255, 255, 255 },
                    new int[3] { 255, 0, 0 },
                    new int[3] { 255, 165, 0 },
                    new int[3] { 255, 255, 0 },
                    new int[3] { 0, 0, 255 },
                    new int[3] { 20, 20, 20 },
                };

                var smokeColors = new MenuListItem("烟雾轨迹颜色", smokeColorsList, 0, "选择一个烟雾轨迹颜色.然后按选择来更改它.更改颜色需要 4 秒钟.在颜色更改期间无法使用烟雾.");

                parachuteMenu.AddMenuItem(togglePrimary);
                parachuteMenu.AddMenuItem(toggleReserve);
                parachuteMenu.AddMenuItem(autoEquipParachutes);
                parachuteMenu.AddMenuItem(unlimitedParachutes);
                parachuteMenu.AddMenuItem(smokeColors);
                parachuteMenu.AddMenuItem(primaryChutes);
                parachuteMenu.AddMenuItem(secondaryChutes);

                parachuteMenu.OnItemSelect += (sender, item, index) =>
                {
                    if (item == togglePrimary)
                    {
                        if (HasPedGotWeapon(Game.PlayerPed.Handle, (uint)GetHashKey("gadget_parachute"), false))
                        {
                            Subtitle.Custom("主降落伞已移除.");
                            RemoveWeaponFromPed(Game.PlayerPed.Handle, (uint)GetHashKey("gadget_parachute"));
                        }
                        else
                        {
                            Subtitle.Custom("主降落伞已添加.");
                            GiveWeaponToPed(Game.PlayerPed.Handle, (uint)GetHashKey("gadget_parachute"), 0, false, false);
                        }
                    }
                    else if (item == toggleReserve)
                    {
                        SetPlayerHasReserveParachute(Game.Player.Handle);
                        Subtitle.Custom("备用降落伞已添加.");
                    }
                };

                parachuteMenu.OnCheckboxChange += (sender, item, index, _checked) =>
                {
                    if (item == unlimitedParachutes)
                    {
                        UnlimitedParachutes = _checked;
                    }
                    else if (item == autoEquipParachutes)
                    {
                        AutoEquipChute = _checked;
                    }
                };

                var switching = false;
                async void IndexChangedEventHandler(Menu sender, MenuListItem item, int oldIndex, int newIndex, int itemIndex)
                {
                    if (item == smokeColors && oldIndex == -1)
                    {
                        if (!switching)
                        {
                            switching = true;
                            SetPlayerCanLeaveParachuteSmokeTrail(Game.Player.Handle, false);
                            await Delay(4000);
                            var color = colors[newIndex];
                            SetPlayerParachuteSmokeTrailColor(Game.Player.Handle, color[0], color[1], color[2]);
                            SetPlayerCanLeaveParachuteSmokeTrail(Game.Player.Handle, newIndex != 0);
                            switching = false;
                        }
                    }
                    else if (item == primaryChutes)
                    {
                        item.Description = $"主降落伞: {chuteDescriptions[newIndex]}";
                        SetPlayerParachuteTintIndex(Game.Player.Handle, newIndex);
                    }
                    else if (item == secondaryChutes)
                    {
                        item.Description = $"备用降落伞: {chuteDescriptions[newIndex]}";
                        SetPlayerReserveParachuteTintIndex(Game.Player.Handle, newIndex);
                    }
                }

                parachuteMenu.OnListItemSelect += (sender, item, index, itemIndex) => IndexChangedEventHandler(sender, item, -1, index, itemIndex);
                parachuteMenu.OnListIndexChange += IndexChangedEventHandler;
            }
            #endregion

            #region Create Weapon Category Submenus
            var spacer = GetSpacerMenuItem("↓ 武器种类 ↓");
            menu.AddMenuItem(spacer);

            var handGuns = new Menu("武器", "手枪");
            var handGunsBtn = new MenuItem("手枪");

            var rifles = new Menu("武器", "突击步枪");
            var riflesBtn = new MenuItem("突击步枪");

            var shotguns = new Menu("武器", "霰弹枪");
            var shotgunsBtn = new MenuItem("霰弹枪");

            var smgs = new Menu("武器", "冲锋枪 / 轻机枪");
            var smgsBtn = new MenuItem("冲锋枪 / 轻机枪");

            var throwables = new Menu("武器", "投掷武器");
            var throwablesBtn = new MenuItem("投掷武器");

            var melee = new Menu("武器", "近战武器");
            var meleeBtn = new MenuItem("近战武器");

            var heavy = new Menu("武器", "重型武器");
            var heavyBtn = new MenuItem("重型武器");

            var snipers = new Menu("武器", "狙击步枪");
            var snipersBtn = new MenuItem("狙击步枪");

            MenuController.AddSubmenu(menu, handGuns);
            MenuController.AddSubmenu(menu, rifles);
            MenuController.AddSubmenu(menu, shotguns);
            MenuController.AddSubmenu(menu, smgs);
            MenuController.AddSubmenu(menu, throwables);
            MenuController.AddSubmenu(menu, melee);
            MenuController.AddSubmenu(menu, heavy);
            MenuController.AddSubmenu(menu, snipers);
            #endregion

            #region Setup weapon category buttons and submenus.
            handGunsBtn.Label = "→→→";
            menu.AddMenuItem(handGunsBtn);
            MenuController.BindMenuItem(menu, handGuns, handGunsBtn);

            riflesBtn.Label = "→→→";
            menu.AddMenuItem(riflesBtn);
            MenuController.BindMenuItem(menu, rifles, riflesBtn);

            shotgunsBtn.Label = "→→→";
            menu.AddMenuItem(shotgunsBtn);
            MenuController.BindMenuItem(menu, shotguns, shotgunsBtn);

            smgsBtn.Label = "→→→";
            menu.AddMenuItem(smgsBtn);
            MenuController.BindMenuItem(menu, smgs, smgsBtn);

            throwablesBtn.Label = "→→→";
            menu.AddMenuItem(throwablesBtn);
            MenuController.BindMenuItem(menu, throwables, throwablesBtn);

            meleeBtn.Label = "→→→";
            menu.AddMenuItem(meleeBtn);
            MenuController.BindMenuItem(menu, melee, meleeBtn);

            heavyBtn.Label = "→→→";
            menu.AddMenuItem(heavyBtn);
            MenuController.BindMenuItem(menu, heavy, heavyBtn);

            snipersBtn.Label = "→→→";
            menu.AddMenuItem(snipersBtn);
            MenuController.BindMenuItem(menu, snipers, snipersBtn);
            #endregion

            #region Loop through all weapons, create menus for them and add all menu items and handle events.
            foreach (var weapon in ValidWeapons.WeaponList)
            {
                var cat = (uint)GetWeapontypeGroup(weapon.Hash);
                if (!string.IsNullOrEmpty(weapon.Name) && IsAllowed(weapon.Perm))
                {
                    //Log($"[DEBUG LOG] [WEAPON-BUG] {weapon.Name} - {weapon.Perm} = {IsAllowed(weapon.Perm)} & All = {IsAllowed(Permission.WPGetAll)}");
                    #region Create menu for this weapon and add buttons
                    var weaponMenu = new Menu("武器功能选项", weapon.Name)
                    {
                        ShowWeaponStatsPanel = true
                    };
                    var stats = new Game.WeaponHudStats();
                    Game.GetWeaponHudStats(weapon.Hash, ref stats);
                    weaponMenu.SetWeaponStats(stats.hudDamage / 100f, stats.hudSpeed / 100f, stats.hudAccuracy / 100f, stats.hudRange / 100f);
                    var weaponItem = new MenuItem(weapon.Name, $"查看 ~y~{weapon.Name}~s~ 详细功能选项.")
                    {
                        Label = "→→→",
                        LeftIcon = MenuItem.Icon.GUN,
                        ItemData = stats
                    };

                    weaponInfo.Add(weaponMenu, weapon);

                    var getOrRemoveWeapon = new MenuItem("装备/移除武器", "将此武器添加到/从你的库存中移除.")
                    {
                        LeftIcon = MenuItem.Icon.GUN
                    };
                    weaponMenu.AddMenuItem(getOrRemoveWeapon);
                    if (!IsAllowed(Permission.WPSpawn))
                    {
                        getOrRemoveWeapon.Enabled = false;
                        getOrRemoveWeapon.Description = "你没有权限使用此选项.";
                        getOrRemoveWeapon.LeftIcon = MenuItem.Icon.LOCK;
                    }

                    var fillAmmo = new MenuItem("弹药补充", "为此武器获取最大弹药.")
                    {
                        LeftIcon = MenuItem.Icon.AMMO
                    };
                    weaponMenu.AddMenuItem(fillAmmo);

                    var tints = new List<string>();
                    if (weapon.Name.Contains(" Mk II"))
                    {
                        foreach (var tint in ValidWeapons.WeaponTintsMkII)
                        {
                            tints.Add(tint.Key);
                        }
                    }
                    else
                    {
                        foreach (var tint in ValidWeapons.WeaponTints)
                        {
                            tints.Add(tint.Key);
                        }
                    }

                    var weaponTints = new MenuListItem("色调", tints, 0, "选择适合武器的色调.");
                    weaponMenu.AddMenuItem(weaponTints);
                    #endregion

                    #region Handle weapon specific list changes
                    weaponMenu.OnListIndexChange += (sender, item, oldIndex, newIndex, itemIndex) =>
                    {
                        if (item == weaponTints)
                        {
                            if (HasPedGotWeapon(Game.PlayerPed.Handle, weaponInfo[sender].Hash, false))
                            {
                                SetPedWeaponTintIndex(Game.PlayerPed.Handle, weaponInfo[sender].Hash, newIndex);
                            }
                            else
                            {
                                Notify.Error("您需要先拿到武器!");
                            }
                        }
                    };
                    #endregion

                    #region Handle weapon specific button presses
                    weaponMenu.OnItemSelect += (sender, item, index) =>
                    {
                        var info = weaponInfo[sender];
                        var hash = info.Hash;

                        SetCurrentPedWeapon(Game.PlayerPed.Handle, hash, true);

                        if (item == getOrRemoveWeapon)
                        {
                            if (HasPedGotWeapon(Game.PlayerPed.Handle, hash, false))
                            {
                                RemoveWeaponFromPed(Game.PlayerPed.Handle, hash);
                                Subtitle.Custom("武器已移除.");
                            }
                            else
                            {
                                var ammo = 255;
                                GetMaxAmmo(Game.PlayerPed.Handle, hash, ref ammo);
                                GiveWeaponToPed(Game.PlayerPed.Handle, hash, ammo, false, true);
                                Subtitle.Custom("武器已添加.");
                            }
                        }
                        else if (item == fillAmmo)
                        {
                            if (HasPedGotWeapon(Game.PlayerPed.Handle, hash, false))
                            {
                                var ammo = 900;
                                GetMaxAmmo(Game.PlayerPed.Handle, hash, ref ammo);
                                SetPedAmmo(Game.PlayerPed.Handle, hash, ammo);
                            }
                            else
                            {
                                Notify.Error("在重新填充弹药之前, 您需要先获得武器!");
                            }
                        }
                    };
                    #endregion

                    #region load components
                    if (weapon.Components != null)
                    {
                        if (weapon.Components.Count > 0)
                        {
                            foreach (var comp in weapon.Components)
                            {
                                //Log($"{weapon.Name} : {comp.Key}");
                                var compItem = new MenuItem(comp.Key, "单击以装备或移除此组件.");
                                weaponComponents.Add(compItem, comp.Key);
                                weaponMenu.AddMenuItem(compItem);

                                #region Handle component button presses
                                weaponMenu.OnItemSelect += (sender, item, index) =>
                                {
                                    if (item == compItem)
                                    {
                                        var Weapon = weaponInfo[sender];
                                        var componentHash = Weapon.Components[weaponComponents[item]];
                                        if (HasPedGotWeapon(Game.PlayerPed.Handle, Weapon.Hash, false))
                                        {
                                            SetCurrentPedWeapon(Game.PlayerPed.Handle, Weapon.Hash, true);
                                            if (HasPedGotWeaponComponent(Game.PlayerPed.Handle, Weapon.Hash, componentHash))
                                            {
                                                RemoveWeaponComponentFromPed(Game.PlayerPed.Handle, Weapon.Hash, componentHash);

                                                Subtitle.Custom("武器组件已移除.");
                                            }
                                            else
                                            {
                                                var ammo = GetAmmoInPedWeapon(Game.PlayerPed.Handle, Weapon.Hash);

                                                var clipAmmo = GetMaxAmmoInClip(Game.PlayerPed.Handle, Weapon.Hash, false);
                                                GetAmmoInClip(Game.PlayerPed.Handle, Weapon.Hash, ref clipAmmo);

                                                GiveWeaponComponentToPed(Game.PlayerPed.Handle, Weapon.Hash, componentHash);

                                                SetAmmoInClip(Game.PlayerPed.Handle, Weapon.Hash, clipAmmo);

                                                SetPedAmmo(Game.PlayerPed.Handle, Weapon.Hash, ammo);
                                                Subtitle.Custom("武器组件已装备.");
                                            }
                                        }
                                        else
                                        {
                                            Notify.Error("您需要先获得武器, 然后才能对其进行改装.");
                                        }
                                    }
                                };
                                #endregion
                            }
                        }
                    }
                    #endregion

                    // refresh and add to menu.
                    weaponMenu.RefreshIndex();

                    if (cat == 970310034) // 970310034 rifles
                    {
                        MenuController.AddSubmenu(rifles, weaponMenu);
                        MenuController.BindMenuItem(rifles, weaponMenu, weaponItem);
                        rifles.AddMenuItem(weaponItem);
                    }
                    else if (cat is 416676503 or 690389602) // 416676503 hand guns // 690389602 stun gun
                    {
                        MenuController.AddSubmenu(handGuns, weaponMenu);
                        MenuController.BindMenuItem(handGuns, weaponMenu, weaponItem);
                        handGuns.AddMenuItem(weaponItem);
                    }
                    else if (cat == 860033945) // 860033945 shotguns
                    {
                        MenuController.AddSubmenu(shotguns, weaponMenu);
                        MenuController.BindMenuItem(shotguns, weaponMenu, weaponItem);
                        shotguns.AddMenuItem(weaponItem);
                    }
                    else if (cat is 3337201093 or 1159398588) // 3337201093 sub machine guns // 1159398588 light machine guns
                    {
                        MenuController.AddSubmenu(smgs, weaponMenu);
                        MenuController.BindMenuItem(smgs, weaponMenu, weaponItem);
                        smgs.AddMenuItem(weaponItem);
                    }
                    else if (cat is 1548507267 or 4257178988 or 1595662460) // 1548507267 throwables // 4257178988 fire extinghuiser // jerry can
                    {
                        MenuController.AddSubmenu(throwables, weaponMenu);
                        MenuController.BindMenuItem(throwables, weaponMenu, weaponItem);
                        throwables.AddMenuItem(weaponItem);
                    }
                    else if (cat is 3566412244 or 2685387236) // 3566412244 melee weapons // 2685387236 knuckle duster
                    {
                        MenuController.AddSubmenu(melee, weaponMenu);
                        MenuController.BindMenuItem(melee, weaponMenu, weaponItem);
                        melee.AddMenuItem(weaponItem);
                    }
                    else if (cat == 2725924767) // 2725924767 heavy weapons
                    {
                        MenuController.AddSubmenu(heavy, weaponMenu);
                        MenuController.BindMenuItem(heavy, weaponMenu, weaponItem);
                        heavy.AddMenuItem(weaponItem);
                    }
                    else if (cat == 3082541095) // 3082541095 sniper rifles
                    {
                        MenuController.AddSubmenu(snipers, weaponMenu);
                        MenuController.BindMenuItem(snipers, weaponMenu, weaponItem);
                        snipers.AddMenuItem(weaponItem);
                    }
                }
            }
            #endregion

            #region Disable submenus if no weapons in that category are allowed.
            if (handGuns.Size == 0)
            {
                handGunsBtn.LeftIcon = MenuItem.Icon.LOCK;
                handGunsBtn.Description = "服务器拥有者已移除此类别所有武器的权限.";
                handGunsBtn.Enabled = false;
            }
            if (rifles.Size == 0)
            {
                riflesBtn.LeftIcon = MenuItem.Icon.LOCK;
                riflesBtn.Description = "服务器拥有者已移除此类别所有武器的权限.";
                riflesBtn.Enabled = false;
            }
            if (shotguns.Size == 0)
            {
                shotgunsBtn.LeftIcon = MenuItem.Icon.LOCK;
                shotgunsBtn.Description = "服务器拥有者已移除此类别所有武器的权限.";
                shotgunsBtn.Enabled = false;
            }
            if (smgs.Size == 0)
            {
                smgsBtn.LeftIcon = MenuItem.Icon.LOCK;
                smgsBtn.Description = "服务器拥有者已移除此类别所有武器的权限.";
                smgsBtn.Enabled = false;
            }
            if (throwables.Size == 0)
            {
                throwablesBtn.LeftIcon = MenuItem.Icon.LOCK;
                throwablesBtn.Description = "服务器拥有者已移除此类别所有武器的权限.";
                throwablesBtn.Enabled = false;
            }
            if (melee.Size == 0)
            {
                meleeBtn.LeftIcon = MenuItem.Icon.LOCK;
                meleeBtn.Description = "服务器拥有者已移除此类别所有武器的权限.";
                meleeBtn.Enabled = false;
            }
            if (heavy.Size == 0)
            {
                heavyBtn.LeftIcon = MenuItem.Icon.LOCK;
                heavyBtn.Description = "服务器拥有者已移除此类别所有武器的权限.";
                heavyBtn.Enabled = false;
            }
            if (snipers.Size == 0)
            {
                snipersBtn.LeftIcon = MenuItem.Icon.LOCK;
                snipersBtn.Description = "服务器拥有者已移除此类别所有武器的权限.";
                snipersBtn.Enabled = false;
            }
            #endregion

            #region Handle button presses
            menu.OnItemSelect += (sender, item, index) =>
            {
                var ped = new Ped(Game.PlayerPed.Handle);
                if (item == getAllWeapons)
                {
                    foreach (var vw in ValidWeapons.WeaponList)
                    {
                        if (IsAllowed(vw.Perm))
                        {
                            GiveWeaponToPed(Game.PlayerPed.Handle, vw.Hash, vw.GetMaxAmmo, false, true);

                            var ammoInClip = GetMaxAmmoInClip(Game.PlayerPed.Handle, vw.Hash, false);
                            SetAmmoInClip(Game.PlayerPed.Handle, vw.Hash, ammoInClip);
                            var ammo = 0;
                            GetMaxAmmo(Game.PlayerPed.Handle, vw.Hash, ref ammo);
                            SetPedAmmo(Game.PlayerPed.Handle, vw.Hash, ammo);
                        }
                    }

                    SetCurrentPedWeapon(Game.PlayerPed.Handle, (uint)GetHashKey("weapon_unarmed"), true);
                }
                else if (item == removeAllWeapons)
                {
                    ped.Weapons.RemoveAll();
                }
                else if (item == setAmmo)
                {
                    SetAllWeaponsAmmo();
                }
                else if (item == refillMaxAmmo)
                {
                    foreach (var vw in ValidWeapons.WeaponList)
                    {
                        if (HasPedGotWeapon(Game.PlayerPed.Handle, vw.Hash, false))
                        {
                            var ammoInClip = GetMaxAmmoInClip(Game.PlayerPed.Handle, vw.Hash, false);
                            SetAmmoInClip(Game.PlayerPed.Handle, vw.Hash, ammoInClip);
                            var ammo = 0;
                            GetMaxAmmo(Game.PlayerPed.Handle, vw.Hash, ref ammo);
                            SetPedAmmo(Game.PlayerPed.Handle, vw.Hash, ammo);
                        }
                    }
                }
                else if (item == spawnByName)
                {
                    SpawnCustomWeapon();
                }
            };
            #endregion

            #region Handle checkbox changes
            menu.OnCheckboxChange += (sender, item, index, _checked) =>
            {
                if (item == noReload)
                {
                    NoReload = _checked;
                    Subtitle.Custom($"无需装弹现在已{(_checked ? "启用" : "禁用")}.");
                }
                else if (item == unlimitedAmmo)
                {
                    UnlimitedAmmo = _checked;
                    Subtitle.Custom($"无限弹药现在已{(_checked ? "启用" : "禁用")}.");
                }
            };
            #endregion

            void OnIndexChange(Menu m, MenuItem i)
            {
                if (i.ItemData is Game.WeaponHudStats stats)
                {
                    m.SetWeaponStats(stats.hudDamage / 100f, stats.hudSpeed / 100f, stats.hudAccuracy / 100f, stats.hudRange / 100f);
                    m.ShowWeaponStatsPanel = true;
                }
                else
                {
                    m.ShowWeaponStatsPanel = false;
                }
            }

            handGuns.OnIndexChange += (sender, oldItem, newItem, oldIndex, newIndex) => { OnIndexChange(sender, newItem); };
            rifles.OnIndexChange += (sender, oldItem, newItem, oldIndex, newIndex) => { OnIndexChange(sender, newItem); };
            shotguns.OnIndexChange += (sender, oldItem, newItem, oldIndex, newIndex) => { OnIndexChange(sender, newItem); };
            smgs.OnIndexChange += (sender, oldItem, newItem, oldIndex, newIndex) => { OnIndexChange(sender, newItem); };
            throwables.OnIndexChange += (sender, oldItem, newItem, oldIndex, newIndex) => { OnIndexChange(sender, newItem); };
            melee.OnIndexChange += (sender, oldItem, newItem, oldIndex, newIndex) => { OnIndexChange(sender, newItem); };
            heavy.OnIndexChange += (sender, oldItem, newItem, oldIndex, newIndex) => { OnIndexChange(sender, newItem); };
            snipers.OnIndexChange += (sender, oldItem, newItem, oldIndex, newIndex) => { OnIndexChange(sender, newItem); };

            handGuns.OnMenuOpen += (sender) => { OnIndexChange(sender, sender.GetCurrentMenuItem()); };
            rifles.OnMenuOpen += (sender) => { OnIndexChange(sender, sender.GetCurrentMenuItem()); };
            shotguns.OnMenuOpen += (sender) => { OnIndexChange(sender, sender.GetCurrentMenuItem()); };
            smgs.OnMenuOpen += (sender) => { OnIndexChange(sender, sender.GetCurrentMenuItem()); };
            throwables.OnMenuOpen += (sender) => { OnIndexChange(sender, sender.GetCurrentMenuItem()); };
            melee.OnMenuOpen += (sender) => { OnIndexChange(sender, sender.GetCurrentMenuItem()); };
            heavy.OnMenuOpen += (sender) => { OnIndexChange(sender, sender.GetCurrentMenuItem()); };
            snipers.OnMenuOpen += (sender) => { OnIndexChange(sender, sender.GetCurrentMenuItem()); };
        }


        #endregion

        /// <summary>
        /// Create the menu if it doesn't exist, and then returns it.
        /// </summary>
        /// <returns>The Menu</returns>
        public Menu GetMenu()
        {
            if (menu == null)
            {
                CreateMenu();
            }
            return menu;
        }
    }
}