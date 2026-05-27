using System;
using System.Text;
using NiumaQuest.Controller;
using NiumaQuest.RuntimeData;
using NiumaSave.Controller;
using NiumaSave.Data;
using NiumaSave.Provider;
using UnityEngine;

namespace NiumaQuest.Save
{
    /// <summary>
    /// NiumaQuest 存档桥接器。
    /// 负责把任务模块快照转换为 NiumaSave 的 Section 数据，并在读档时导回任务控制器。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NiumaQuestSaveAdapter : MonoBehaviour, ISaveDataProvider
    {
        private const string QuestSectionId = "quest";
        private const string QuestSectionVersion = "1";
        private const string QuestSectionFormat = "json";

        [Header("模块引用")]
        [Tooltip("任务模块根控制器。请拖入场景中的 NiumaQuestController。")]
        [SerializeField] private NiumaQuestController questController;

        [Tooltip("存档模块根控制器。开启自动注册时，请拖入场景中的 NiumaSaveController。")]
        [SerializeField] private NiumaSaveController saveController;

        [Header("注册行为")]
        [Tooltip("启用组件时是否自动注册到 NiumaSaveController。正式场景建议开启。")]
        [SerializeField] private bool registerOnEnable = true;

        [Tooltip("引用为空时是否自动在场景中查找对应控制器。调试阶段可以开启，正式场景建议手动绑定。")]
        [SerializeField] private bool autoFindControllers = true;

        private bool _registeredToSaveController;

        /// <summary>
        /// 任务模块的稳定存档段 ID。
        /// </summary>
        public string SectionId => QuestSectionId;

        /// <summary>
        /// 任务存档段结构版本。
        /// </summary>
        public string SectionVersion => QuestSectionVersion;

        /// <summary>
        /// 任务数据版本号。
        /// NiumaSave 通过该值判断任务模块是否发生变化。
        /// </summary>
        public long Revision => questController != null ? questController.QuestRevision : 0L;

        private void Awake()
        {
            ResolveControllers(false);
        }

        private void OnEnable()
        {
            if (!registerOnEnable)
            {
                return;
            }

            RegisterToSaveController();
        }

        private void OnDisable()
        {
            if (_registeredToSaveController && saveController != null)
            {
                saveController.UnregisterProvider(SectionId);
            }

            _registeredToSaveController = false;
        }

        /// <summary>
        /// 导出任务快照为 NiumaSave Section。
        /// 这里只序列化 QuestProgressSnapshot，不直接序列化 QuestRuntimeState。
        /// </summary>
        public SaveSectionData ExportSection()
        {
            ResolveControllers(false);
            if (questController == null)
            {
                throw new InvalidOperationException("NiumaQuestSaveAdapter 缺少 NiumaQuestController，无法导出任务存档。");
            }

            var saveData = new QuestSaveData
            {
                Quests = questController.ExportSnapshots() ?? Array.Empty<QuestProgressSnapshot>()
            };

            var json = JsonUtility.ToJson(saveData);
            var bytes = Encoding.UTF8.GetBytes(json);
            return new SaveSectionData
            {
                SectionId = SectionId,
                SectionVersion = SectionVersion,
                Format = QuestSectionFormat,
                DataEncoding = SaveDataEncoding.Base64,
                EncodedData = Convert.ToBase64String(bytes)
            };
        }

        /// <summary>
        /// 从 NiumaSave Section 导入任务快照。
        /// 失败时返回结构化错误，阻止坏数据静默进入任务系统。
        /// </summary>
        public SaveSectionImportResult ImportSection(SaveSectionData section)
        {
            ResolveControllers(false);
            if (questController == null)
            {
                return SaveSectionImportResult.Fail(
                    SaveSectionImportErrorCode.ImportFailed,
                    "NiumaQuestSaveAdapter 缺少 NiumaQuestController，无法导入任务存档。");
            }

            if (section == null)
            {
                return SaveSectionImportResult.Fail(SaveSectionImportErrorCode.NullSection, "任务存档段为空。");
            }

            if (!string.Equals(section.SectionId, SectionId, StringComparison.Ordinal))
            {
                return SaveSectionImportResult.Fail(
                    SaveSectionImportErrorCode.SectionIdMismatch,
                    $"任务存档段 ID 不匹配：expected={SectionId}, actual={section.SectionId}");
            }

            if (!string.Equals(section.SectionVersion, SectionVersion, StringComparison.Ordinal))
            {
                return SaveSectionImportResult.Fail(
                    SaveSectionImportErrorCode.VersionUnsupported,
                    $"任务存档段版本不支持：{section.SectionVersion}");
            }

            if (!string.Equals(section.DataEncoding, SaveDataEncoding.Base64, StringComparison.Ordinal))
            {
                return SaveSectionImportResult.Fail(
                    SaveSectionImportErrorCode.DataCorrupted,
                    $"任务存档段编码不支持：{section.DataEncoding}");
            }

            if (string.IsNullOrWhiteSpace(section.EncodedData))
            {
                return SaveSectionImportResult.Fail(SaveSectionImportErrorCode.DataCorrupted, "任务存档段数据为空。");
            }

            try
            {
                var bytes = Convert.FromBase64String(section.EncodedData);
                var json = Encoding.UTF8.GetString(bytes);
                var saveData = JsonUtility.FromJson<QuestSaveData>(json);
                questController.ImportSnapshots(saveData?.Quests ?? Array.Empty<QuestProgressSnapshot>());
                return SaveSectionImportResult.Success();
            }
            catch (Exception ex)
            {
                return SaveSectionImportResult.Fail(
                    SaveSectionImportErrorCode.DataCorrupted,
                    $"任务存档段解析失败：{ex.Message}");
            }
        }

        [ContextMenu("NiumaQuestSave/注册到存档模块")]
        private void RegisterToSaveController()
        {
            if (_registeredToSaveController)
            {
                return;
            }

            ResolveControllers(true);
            if (saveController == null)
            {
                return;
            }

            var registered = saveController.RegisterProvider(this);
            _registeredToSaveController = registered;
            if (!registered)
            {
                UnityEngine.Debug.LogWarning("[NiumaQuestSaveAdapter] 注册任务存档 Provider 失败。", this);
            }
        }

        [ContextMenu("NiumaQuestSave/从存档模块取消注册")]
        private void UnregisterFromSaveController()
        {
            ResolveControllers(false);
            if (_registeredToSaveController && saveController != null)
            {
                saveController.UnregisterProvider(SectionId);
            }

            _registeredToSaveController = false;
        }

        private void ResolveControllers(bool logMissing)
        {
            if (!autoFindControllers)
            {
                return;
            }

            if (questController == null)
            {
#if UNITY_2023_1_OR_NEWER
                questController = FindFirstObjectByType<NiumaQuestController>();
#else
                questController = FindObjectOfType<NiumaQuestController>();
#endif
            }

            if (saveController == null)
            {
#if UNITY_2023_1_OR_NEWER
                saveController = FindFirstObjectByType<NiumaSaveController>();
#else
                saveController = FindObjectOfType<NiumaSaveController>();
#endif
            }

            if (logMissing && questController == null)
            {
                UnityEngine.Debug.LogWarning("[NiumaQuestSaveAdapter] 未找到 NiumaQuestController，请在 Inspector 中绑定。", this);
            }

            if (logMissing && saveController == null)
            {
                UnityEngine.Debug.LogWarning("[NiumaQuestSaveAdapter] 未找到 NiumaSaveController，请在 Inspector 中绑定。", this);
            }
        }
    }
}
