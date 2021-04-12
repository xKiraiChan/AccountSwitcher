using MelonLoader;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using VRC.Core;

[assembly: MelonInfo(typeof(KiraiMod.AccountSwitcher), "AccountSwitcher", "0.1.0", "Kirai Chan#8315", "github.com/xKiraiChan/AccountSwitcher")]
[assembly: MelonGame("VRChat", "VRChat")]

namespace KiraiMod
{
    public class AccountSwitcher : MelonMod
    {
        public const string freshPath = "UserData/kAccounts.txt";
        public const string stalePath = "UserData/kAccounts.used.txt";
        public const string prevPath = "UserData/kAccounts.prev.txt";

        public int total;
        public int fresh;
        public int stale;

        private string[] accountsNew;
        private string[] accountsOld;

        public override void OnApplicationStart() => Deserialize();
        public override void OnApplicationQuit() => Serialize();

        public override void OnGUI()
        {
            if (!Input.GetKey(KeyCode.Tab)) return;

            GUI.Label(new Rect(340, 5, 200, 20), $"Statistics: <color=#0ff>{total}</color> | <color=#0f0>{fresh}</color> | <color=#f00>{stale}</color>");

            if (GUI.Button(new Rect(340, 30, 150, 20), "Switch account")) MelonCoroutines.Start(EnterAccount(GetAccount()));
            if (GUI.Button(new Rect(340, 50, 150, 20), "Switch previous")) MelonCoroutines.Start(EnterAccount(GetPrevious()));
            if (GUI.Button(new Rect(340, 70, 150, 20), "Reload accounts")) Deserialize();
        }

        public System.Collections.IEnumerator EnterAccount((string, string) account)
        {
            if (account.Item1 is null) yield break;

            if (APIUser.CurrentUser != null)
            {
                // open up the settings panel
                QuickMenu.prop_QuickMenu_0.transform.Find("ShortcutMenu/SettingsButton").GetComponent<Button>().onClick.Invoke();
                yield return new WaitForSeconds(1);

                GameObject.Find("UserInterface/MenuContent/Screens/Settings/Footer/Logout").GetComponent<Button>().onClick.Invoke();
                yield return new WaitForSeconds(3);
            }

            // click the login with vrchat account button
            GameObject.Find("UserInterface/MenuContent/Screens/Authentication/StoreLoginPrompt/VRChatButtonLogin").GetComponent<Button>().onClick.Invoke();
            yield return new WaitForSeconds(1);

            // enter the username
            GameObject.Find("UserInterface/MenuContent/Popups/InputPopup/InputField").GetComponent<InputField>().text = account.Item1;
            yield return new WaitForSeconds(1);

            // confirm the username
            GameObject.Find("UserInterface/MenuContent/Popups/InputPopup/ButtonRight").GetComponent<Button>().onClick.Invoke();
            yield return new WaitForSeconds(1);

            // enter the password
            GameObject.Find("UserInterface/MenuContent/Popups/InputPopup/InputField").GetComponent<InputField>().text = account.Item2;
            yield return new WaitForSeconds(1);

            // confirm the password
            GameObject.Find("UserInterface/MenuContent/Popups/InputPopup/ButtonRight").GetComponent<Button>().onClick.Invoke();
        }

        public (string, string) GetPrevious()
        {
            if (!File.Exists(prevPath))
            {
                MelonLogger.Warning("Previous account does not exist");
                return (null, null);
            }

            string account = File.ReadAllText(prevPath);

            string[] split = account.Split(':');

            if (split.Length != 2)
            {
                MelonLogger.Warning($"UserPass has unexpected length of {split.Length}");
                return (null, null);
            }

            return (split[0], split[1]);
        }

        public (string, string) GetAccount()
        {
            Deserialize();

            int idx = Random.Range(0, accountsNew.Length);

            string account = accountsNew[idx];

            List<string> tmp = accountsNew.ToList();
            tmp.RemoveAt(idx);
            accountsNew = tmp.ToArray();

            tmp = accountsOld.ToList();
            tmp.Add(account);
            accountsOld = tmp.ToArray();

            fresh--;
            stale++;

            Serialize();

            File.WriteAllText(prevPath, account);

            string[] split = account.Split(':');

            if (split.Length != 2)
            {
                MelonLogger.Warning($"UserPass has unexpected length of {split.Length}");
                return (null, null);
            }

            return (split[0], split[1]);
        }

        public void Deserialize()
        {
            if (!File.Exists(freshPath))
                File.Create(freshPath);

            if (!File.Exists(stalePath))
                File.Create(stalePath);

            accountsNew = File.ReadAllLines(freshPath);
            accountsOld = File.ReadAllLines(stalePath);

            fresh = accountsNew.Length;
            stale = accountsOld.Length;
            total = fresh + stale;
        }

        public void Serialize()
        {
            File.WriteAllLines(freshPath, accountsNew);
            File.WriteAllLines(stalePath, accountsOld);
        }
    }
}
