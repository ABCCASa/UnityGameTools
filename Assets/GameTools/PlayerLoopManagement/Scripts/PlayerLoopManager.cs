using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.LowLevel;
using static UnityEngine.LowLevel.PlayerLoopSystem;

namespace GameTools.PlayerLoopManagement
{
    public static class PlayerLoopManager
    {
        public static void ResetPlayerLoop()
        {
            var loop = PlayerLoop.GetDefaultPlayerLoop();
            PlayerLoop.SetPlayerLoop(loop);
        }

        public static bool InsertAdjacentTo<TNewSystem, TRelativeSystem>(UpdateFunction action, bool addAfter)
        {
            var root = PlayerLoop.GetCurrentPlayerLoop();
            if (InsertAdjacentTo<TNewSystem, TRelativeSystem>(ref root, action, addAfter))
            {
                PlayerLoop.SetPlayerLoop(root);
                return true;
            }
            return false;
        }

        public static bool Exist<TSystem>() 
        {
            var root = PlayerLoop.GetCurrentPlayerLoop();
            return Exist<TSystem>(ref root);
        }

        private static bool Exist<TSystem>(ref PlayerLoopSystem system)
        {
            if (system.type == typeof(TSystem)) { return true; }

            if (system.subSystemList == null) { return false; }

            for (var i = 0; i < system.subSystemList.Length; i++)
            {
                if (Exist<TSystem>(ref system.subSystemList[i])) { return true; }
            }
            return false;
        }

        private static bool InsertAdjacentTo<TNewSystem, TRelativeSystem>(ref PlayerLoopSystem system, UpdateFunction action, bool addAfter)
        {
            if (system.subSystemList == null)
            {
                return false;
            }

            for (var i = 0; i < system.subSystemList.Length; i++)
            {
                if (InsertAdjacentTo<TNewSystem, TRelativeSystem>(ref system.subSystemList[i], action, addAfter))
                {
                    return true;
                }

                if (system.subSystemList[i].type == typeof(TRelativeSystem))
                {
                    var list = new List<PlayerLoopSystem>(system.subSystemList);

                    var customUpdate = new PlayerLoopSystem()
                    {
                        updateDelegate = action,
                        type = typeof(TNewSystem)
                    };

                    list.Insert(addAfter ? i + 1 : i, customUpdate);
                    system.subSystemList = list.ToArray();
                    return true;
                }
            }
            return false;
        }

        public static bool AddTo<TNewSystem, TRelativeSystem>(UpdateFunction action, bool addAfter)
        {
            var root = PlayerLoop.GetCurrentPlayerLoop();
            if (AddTo<TNewSystem, TRelativeSystem>(ref root, action, addAfter))
            {
                PlayerLoop.SetPlayerLoop(root);
                return true;
            }
            return false;
        }

        private static bool AddTo<TNewSystem, TRelativeSystem>(ref PlayerLoopSystem system, UpdateFunction action, bool addAfter)
        {
            if (system.type == typeof(TRelativeSystem))
            {
                var customUpdate = new PlayerLoopSystem()
                {
                    updateDelegate = action,
                    type = typeof(TNewSystem)
                };

                var list = system.subSystemList == null ? new List<PlayerLoopSystem>() : new List<PlayerLoopSystem>(system.subSystemList);
                if (addAfter)
                {
                    list.Add(customUpdate);
                }
                else
                {
                    list.Insert(0, customUpdate);
                }
                system.subSystemList = list.ToArray();
                return true;
            }

            if (system.subSystemList == null)
            {
                return false;
            }

            for (var i = 0; i < system.subSystemList.Length; i++)
            {
                if (AddTo<TNewSystem, TRelativeSystem>(ref system.subSystemList[i], action, addAfter))
                {
                    return true;
                }
            }
            return false;
        }

        public static void PrintPlayerLoop()
        {
            StringBuilder sb = new();
            RecursivePrintPlayerLoop(PlayerLoop.GetCurrentPlayerLoop(), sb, 0);
            Debug.Log(sb.ToString());
        }

        private static void RecursivePrintPlayerLoop(PlayerLoopSystem playerLoopSystem, StringBuilder sb, int depth)
        {
            if (depth == 0)
            {
                sb.AppendLine("ROOT NODE");
            }
            else if (playerLoopSystem.type != null)
            {
                for (int i = 0; i < depth; i++)
                {
                    sb.Append("\t");
                }
                sb.AppendLine(playerLoopSystem.type.Name);
            }
            if (playerLoopSystem.subSystemList != null)
            {
                depth++;
                foreach (var s in playerLoopSystem.subSystemList)
                {
                    RecursivePrintPlayerLoop(s, sb, depth);
                }
            }
        }
    }
}