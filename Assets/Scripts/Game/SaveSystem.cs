using UnityEngine;

namespace Rewind.Game
{
    public static class SaveSystem
    {
        const string Key = "save_v1";

        public static bool HasSave => PlayerPrefs.HasKey(Key);

        public static GameState Load() => GameState.FromJson(PlayerPrefs.GetString(Key, null));

        public static void Save(GameState state)
        {
            PlayerPrefs.SetString(Key, state.ToJson());
            PlayerPrefs.Save();
        }

        public static void Delete()
        {
            PlayerPrefs.DeleteKey(Key);
            PlayerPrefs.Save();
        }
    }
}
