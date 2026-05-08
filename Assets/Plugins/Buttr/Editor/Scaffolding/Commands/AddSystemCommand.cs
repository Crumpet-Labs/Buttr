namespace Buttr.Editor.Scaffolding {
    internal readonly ref struct AddSystemCommand {
        private readonly string m_PackageFolder;
        private readonly bool m_RefreshAssetDatabase;

        public AddSystemCommand(string packageFolder, bool refreshAssetDatabase) {
            m_PackageFolder = packageFolder;
            m_RefreshAssetDatabase = refreshAssetDatabase;
        }

        public void Execute() {
            var (ns, name) = m_PackageFolder.InferPackage();
            new AddBehaviourCommand(m_PackageFolder, false).Execute();
            var components = m_PackageFolder.EnsureSubFolder("Components");
            components.WriteFileIfNew($"{name}System.cs", new ButtrSystemTemplate(ns, name).Generate(), m_RefreshAssetDatabase);
        }
    }
}