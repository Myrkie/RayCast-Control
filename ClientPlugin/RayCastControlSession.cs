using System.Collections.Generic;
using NLog;
using Sandbox.Engine.Physics;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.GameSystems;
using Sandbox.Game.Gui;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Input;
using VRage.ModAPI;

namespace ClientPlugin
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    // ReSharper disable once UnusedType.Global
    public class RayCastControlSession : MySessionComponentBase
    {
        public override void UpdateBeforeSimulation()
        {
            if (MyAPIGateway.Session?.Player?.Controller?.ControlledEntity == null)
                return;

            HandleHotkey();
        }
        
        // input
        private enum HandleHotkeyType
        {
            AltB,
            AltR,
            AltShiftR,
            AltShiftB
        }

        private bool hotkeyPressedLastFrame;
        private void HandleHotkey()
        {

            var input = MyInput.Static;
            HandleHotkeyType? pressedHotkey = null;

            if (Config.Current.TakeControl.IsPressed(input))
            {
                pressedHotkey = HandleHotkeyType.AltB;
            }
            else if (Config.Current.CyclePower.IsPressed(input))
            {
                pressedHotkey = HandleHotkeyType.AltR;
            }
            else if (Config.Current.ShutdownPower.IsPressed(input))
            {
                pressedHotkey = HandleHotkeyType.AltShiftR;
            }
            else if (Config.Current.AccessTerminal.IsPressed(input))
            {
                pressedHotkey = HandleHotkeyType.AltShiftB;
            }


            if (pressedHotkey.HasValue && !hotkeyPressedLastFrame)
            {
                ToggleRemoteControl(pressedHotkey.Value);
            }

            hotkeyPressedLastFrame = pressedHotkey.HasValue;
        }

        // main logic
        
        private void ToggleRemoteControl(HandleHotkeyType hotKey)
        {
            var player = MyAPIGateway.Session.Player;
            if (player == null) return;
            
            var controlledEntity = player.Controller?.ControlledEntity?.Entity;
            var grid = GetGridLookingAt(1000, out var getGridLookingAtReason);
            if (grid == null)
            {
                WriteToHudAndLog(getGridLookingAtReason, Config.Current.DisappearTimeUrgentMs, MyFontEnum.Red, LogLevel.Debug);
                return;
            }
            switch (controlledEntity)
            {
                case MyCharacter _ when !IsPlayerAntennaBroadcastingPossible(player):
                    WriteToHudAndLog("Cannot control grid: Your suit energy is too low for broadcasting!", 
                        Config.Current.DisappearTimeUrgentMs, MyFontEnum.Red, LogLevel.Debug);
                    return;
                case MyCharacter _ when !IsPlayerBroadcasting(player):
                    WriteToHudAndLog("Cannot control grid: Your antenna is not broadcasting!", 
                        Config.Current.DisappearTimeUrgentMs, MyFontEnum.Red, LogLevel.Debug);
                    return;
                case MyCharacter _ when !IsGridReachableByPlayer(grid, player as MyPlayer):
                    WriteToHudAndLog($"Cannot control grid: {grid.DisplayName} is not reachable!", 
                        Config.Current.DisappearTimeUrgentMs, MyFontEnum.Red, LogLevel.Debug);
                    return;
                case MyCockpit sourceCockpit when !IsGridReachableByGrid(sourceCockpit.CubeGrid, grid, player as MyPlayer):
                    WriteToHudAndLog($"Cannot control grid: {grid.DisplayName} is not reachable from {sourceCockpit.CubeGrid.DisplayName}!", 
                        Config.Current.DisappearTimeUrgentMs, MyFontEnum.Red, LogLevel.Debug);
                    return;
            }
            
            if (!IsGridPowered(grid))
            {
                WriteToHudAndLog($"Cannot control grid: {grid.DisplayName} is not powered!",
                    Config.Current.DisappearTimeUrgentMs, MyFontEnum.Red, LogLevel.Error);
                return;
            }

            DisableMainCockpit(grid as MyCubeGrid);


            switch (hotKey)
            {
                case HandleHotkeyType.AltB:
                    if (GetRemoteControl(grid, player, out var remote)) return;
                    remote.RequestControl();
                    WriteToHudAndLog($"Remote Controlling grid {remote.CubeGrid.DisplayName}", Config.Current.DisappearTimeMinorMs, MyFontEnum.Green, LogLevel.Info);
                    break;
                case HandleHotkeyType.AltR:
                    if (GetRemoteControl(grid, player, out remote)) return;
                    remote.SwitchReactorsLocal();
                    remote.SwitchReactorsLocal();
                    WriteToHudAndLog($"Cycled Reactors on grid {remote.CubeGrid.DisplayName}", Config.Current.DisappearTimeMinorMs, MyFontEnum.Green, LogLevel.Info);
                    break;
                case HandleHotkeyType.AltShiftR:
                    if (GetRemoteControl(grid, player, out remote)) return;
                    remote.SwitchReactorsLocal();
                    WriteToHudAndLog($"Disabled reactors on grid {remote.CubeGrid.DisplayName}", Config.Current.DisappearTimeMinorMs, MyFontEnum.Green, LogLevel.Info);
                    break;
                case HandleHotkeyType.AltShiftB:
                    OpenGridTerminal(grid as MyCubeGrid, player as MyPlayer);
                    WriteToHudAndLog($"Opening terminal on grid {grid.DisplayName}", Config.Current.DisappearTimeMinorMs, MyFontEnum.Green, LogLevel.Info);
                    break;
            }
        }

        private static void DisableMainCockpit(MyCubeGrid grid)
        {
            if (grid.HasMainCockpit())
            {
                grid.MainCockpit = null;
            }
        }

        private static bool GetRemoteControl(IMyCubeGrid grid, IMyPlayer player, out MyRemoteControl remote)
        {
            remote = null;
            
            foreach (var rc in grid.GetFatBlocks<MyRemoteControl>())
            {
                if (rc.IsFunctional) remote = rc;
            }

            if (Config.Current.RecursiveRemote && remote == null)
            {
                HashSet<MyCubeGrid> visited = [];
                MyRemoteControl foundRemote = null;
                
                void SearchGrid(MyCubeGrid currentGrid)
                {
                    RayCastControlPlugin.WriteToPulsarLog($"Searching grid {currentGrid.DisplayName} recursively", LogLevel.Info);
                    if (visited.Contains(currentGrid) || foundRemote != null) return;

                    visited.Add(currentGrid);

                    foreach (var rc in currentGrid.GetFatBlocks<MyRemoteControl>())
                    {
                        if (!rc.IsFunctional) continue;
                        RayCastControlPlugin.WriteToPulsarLog($"found {rc.DisplayName} remote control on grid {currentGrid.DisplayName}", LogLevel.Info);
                        foundRemote = rc;
                        return;
                    }

                    currentGrid.GetConnectedGrids(GridLinkTypeEnum.Physical, SearchGrid);
                }

                SearchGrid(grid as MyCubeGrid);

                remote = foundRemote;
            }
            
            if (remote == null)
            {
                WriteToHudAndLog("No functional remote block found", 2000, MyFontEnum.Red, LogLevel.Error);
                return true;
            }

            if (!remote.HasPlayerAccess(player.IdentityId))
            {
                WriteToHudAndLog(
                    $"You don't have permission to control this grid! Grid is owned by {GetGridOwner(remote)}", Config.Current.DisappearTimeUrgentMs, MyFontEnum.Red, LogLevel.Error);
                return true;
            }
            
            if (MySession.Static.ControlledEntity is MyRemoteControl currentRemote)
            {
                WriteToHudAndLog(
                    $"You are already controlling grid {currentRemote.CubeGrid.DisplayName} via remote {currentRemote.CustomName} connection not possible.",
                    Config.Current.DisappearTimeUrgentMs, MyFontEnum.Red, LogLevel.Error);
                return true;
            }

            if (remote.CanControl(MySession.Static.ControlledEntity)) return false;
            WriteToHudAndLog($"Cannot connect to remote grid {grid.DisplayName}", Config.Current.DisappearTimeUrgentMs, MyFontEnum.Red, LogLevel.Error);
            return true;

        }

        private static void OpenGridTerminal(MyCubeGrid grid, IMyPlayer player)
        {
            var character = GetControlledCharacter(player);
            if (character == null || grid == null)
                return;

            foreach (var fatBlock in grid.GetFatBlocks())
            {
                var cubeBlock = fatBlock;
                if (cubeBlock is not { IsFunctional: true }) continue;
                MyGuiScreenTerminal.Show(
                    MyTerminalPageEnum.ControlPanel,
                    character,
                    cubeBlock,
                    true
                );
                break;
            }
        }
        
        private static void WriteToHudAndLog(string content, int disappearTime, MyFontEnum font, LogLevel logLevel)
        {
            RayCastControlPlugin.WriteToPulsarLog(content, logLevel);
            MyAPIGateway.Utilities.ShowNotification(content, disappearTime,font);
        }
        
        private static string GetGridOwner(IMyCubeBlock remote)
        {
            var steamId = MyAPIGateway.Players.TryGetSteamId(remote.OwnerId);
            return MySession.Static?.Players?.TryGetIdentityNameFromSteamId(steamId);
        }

        private static IMyCubeGrid GetGridLookingAt(double maxDistance, out string reason)
        {
            reason = null;
            var camera = MyAPIGateway.Session.Camera;
            var start = camera.WorldMatrix.Translation;
            var dir = camera.WorldMatrix.Forward;
            var end = start + dir * maxDistance;

            bool hit = MyAPIGateway.Physics.CastRay(start, end, out var hitInfo, MyPhysics.CollisionLayers.DefaultCollisionLayer, true);
            if (!hit || hitInfo?.HitEntity == null)
            {
                reason = "No entity in sight.";
                return null;
            }

            switch (hitInfo.HitEntity)
            {
                case IMyVoxelBase:
                    reason = "Looking at a voxel, not a grid.";
                    return null;
                case IMyCubeBlock block:
                    return block.CubeGrid;
                case IMyCubeGrid grid:
                    return grid;
                default:
                    reason = $"Hit an unsupported entity type. Name: {hitInfo.HitEntity.Name} {hitInfo.GetType()}";
                    return null;
            }
        }

        private static MyCharacter GetControlledCharacter(IMyPlayer player)
        {
            var entity = player.Controller.ControlledEntity?.Entity;
            return entity as MyCharacter;
        }

        private static bool IsPlayerAntennaBroadcastingPossible(IMyPlayer player)
        {
            var playerCharacter = GetControlledCharacter(player);
            var suitEnergyLevel = playerCharacter?.SuitEnergyLevel;
            return suitEnergyLevel > 0;
        }
        private static bool IsPlayerBroadcasting(IMyPlayer player)
        {
            var playerCharacter = GetControlledCharacter(player);
            var radioBroadcaster = playerCharacter?.RadioBroadcaster;
            return radioBroadcaster is { Enabled: true };
        }
        
        private static bool IsGridReachableByPlayer(IMyCubeGrid targetGrid, MyPlayer player)
        {
            if (targetGrid == null)
                return false;

            if (player?.Controller?.ControlledEntity?.Entity is not { } controlledEntity) return false;
            var antennaSystem = MyAntennaSystem.Static;

            bool reachable = antennaSystem.CheckConnection(
                controlledEntity,
                (MyEntity)targetGrid,
                player,
                mutual: true
            );

            var reachableInfo = antennaSystem.GetConnectedGridsInfo(
                (MyEntity)targetGrid,
                player,
                mutual: true,
                accessible: true
            );

            RayCastControlPlugin.WriteToPulsarLog($"Trying to reach {targetGrid.DisplayName} from {controlledEntity.DisplayName}", LogLevel.Info);

            foreach (var info in reachableInfo)
            {
                RayCastControlPlugin.WriteToPulsarLog($"Reachable from {targetGrid.DisplayName} via: {info.Name} ({info.EntityId})", LogLevel.Debug);
            }
            
            RayCastControlPlugin.WriteToPulsarLog(!reachable
                ? $"Connection from {player.DisplayName} to {targetGrid.DisplayName} not found in relayed path."
                : $"Connection path {player.DisplayName} to {targetGrid.DisplayName} confirmed.", LogLevel.Info
            );

            return reachable;
        }
        
        private static bool IsGridReachableByGrid(IMyCubeGrid sourceGrid, IMyCubeGrid targetGrid, MyPlayer player)
        {
            if (sourceGrid == null || targetGrid == null || player == null)
                return false;

            var antennaSystem = MyAntennaSystem.Static;

            bool reachable = antennaSystem.CheckConnection(
                (MyEntity)sourceGrid,
                (MyEntity)targetGrid,
                player,
                mutual: true
            );

            var reachableInfo = antennaSystem.GetConnectedGridsInfo(
                (MyEntity)targetGrid,
                player,
                mutual: true,
                accessible: true
            );

            RayCastControlPlugin.WriteToPulsarLog($"Trying to reach {targetGrid.DisplayName} from {sourceGrid.DisplayName}", LogLevel.Info);

            foreach (var info in reachableInfo)
            {
                RayCastControlPlugin.WriteToPulsarLog($"Reachable from {targetGrid.DisplayName} via: {info.Name} ({info.EntityId})", LogLevel.Debug);
            }

            RayCastControlPlugin.WriteToPulsarLog(!reachable
                    ? $"Connection from {sourceGrid.DisplayName} to {targetGrid.DisplayName} not found in relayed path."
                    : $"Connection path {sourceGrid.DisplayName} to {targetGrid.DisplayName} confirmed.", LogLevel.Info
            );

            return reachable;
        }

        
        private static bool IsGridPowered(IMyCubeGrid grid)
        {
            var myCubeGrid = grid as MyCubeGrid;
            return myCubeGrid is { IsPowered: true };
        }
    }
}
