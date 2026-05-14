internal interface ISettingsStore
{
    Task<List<SyncPreset>> LoadAsync();
    Task SaveOrMergeAsync(SyncConfiguration configuration);
}
