/// -------------------------------------------------------------------------------
/// NovaEngine Framework
///
/// Copyright (C) 2025 - 2026, Hainan Yuanyou Information Technology Co., Ltd. Guangzhou Branch
///
/// Permission is hereby granted, free of charge, to any person obtaining a copy
/// of this software and associated documentation files (the "Software"), to deal
/// in the Software without restriction, including without limitation the rights
/// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
/// copies of the Software, and to permit persons to whom the Software is
/// furnished to do so, subject to the following conditions:
///
/// The above copyright notice and this permission notice shall be included in
/// all copies or substantial portions of the Software.
///
/// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
/// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
/// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
/// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
/// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
/// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
/// THE SOFTWARE.
/// -------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
using System.Threading;

namespace NovaFramework.Editor.Launcher
{
    public class PackageInstallerLauncher
    {

        
        private static readonly Dictionary<string, string> _gitUrlDic = new Dictionary<string, string>()
        {
            {
                "com.novaframework.unity.core.common",
                "https://github.com/yoseasoft/com.novaframework.unity.core.common.git"
            },
            {
                "com.novaframework.unity.installer",
                "https://github.com/AkasLiu/com.novaframework.unity.installer.git"
            },
        };

        private static string _launcherPackageName = "com.novaframework.unity.launcher";

        [InitializeOnLoadMethod] // 当编辑器加载时自动检测是否需要启动安装
        static void OnEditorLoaded()
        {
            // 检查是否已加载必要的程序集
            if (HasNecessaryAssemblies())
            {
                // 延迟执行，确保编辑器完全加载
                EditorApplication.delayCall += () =>
                {
                    Debug.Log("Installation not complete or packages installed but configuration not finished, starting AutoInstallManager...");
                    TryLoadAndStartAutoInstallManager();
                   
                };
            }
            else
            {
                // 如果程序集不存在，说明需要执行安装
                Debug.Log("Required assemblies not found, preparing to download installer and common packages...");
                
                // 自动开始安装流程
                EditorApplication.delayCall += () =>
                {
                    ExecuteInstallation();
                };
            }
        }
        
        static void ExecuteInstallation()
        {
            // 检查Nova.Installer.Editor程序集是否存在
            if (HasNecessaryAssemblies())
            {
                Debug.Log("Nova.Installer.Editor assembly already exists. Starting AutoInstallManager...");
                
                // 注册projectChanged事件，以便在项目完全加载后启动AutoInstallManager
                EditorApplication.projectChanged += OnProjectChangedAfterResolve;
                return;
            }
            
            // 开始新的安装流程 - 直接开始安装，不需要确认
            ContinueInstallation();
        }
        
        // 继续安装流程
        static void ContinueInstallation()
        {
            
            // 显示一个简单的确认对话框
            bool startInstall = EditorUtility.DisplayDialog("开始安装", "是否开始自动安装NovaFramework？\n\n自动安装将会:\n- 下载并安装必要的框架包\n- 配置项目环境\n- 设置所需的资源目录\n\n注意：安装过程可能需要几分钟时间，请耐心等待。", "开始安装", "取消");
            
            if (startInstall)
            {
                Debug.Log("User confirmed installation start");
                
                // 延迟执行安装，确保UI已渲染
                EditorApplication.delayCall += DoExecuteInstallation;
            }
            else
            {
                Debug.Log("User cancelled installation");
            }
        }
        
        // 当项目在Client.Resolve()后发生变化时调用
        private static void OnProjectChangedAfterResolve()
        {
            // 确保只执行一次
            EditorApplication.projectChanged -= OnProjectChangedAfterResolve;
            
            Debug.Log("Project changed after Client.Resolve(), checking installation status...");
            
            // 检查是否已加载必要的程序集
            if (HasNecessaryAssemblies())
            {
                Debug.Log("Required assemblies detected, attempting to start AutoInstallManager...");
                
                // 延迟执行，确保所有资源都已加载完成
                EditorApplication.delayCall += () =>
                {
                    TryLoadAndStartAutoInstallManager();
                };
            }
            else
            {
                Debug.LogWarning("Project changed but required assemblies are still not loaded.");
            }
        }

        static void DoExecuteInstallation()
        {
            try
            {
                string projectPath = Path.GetDirectoryName(Application.dataPath);
                string novaFrameworkDataPath = Path.Combine(projectPath, "NovaFrameworkData");
                string frameworkRepoPath = Path.Combine(novaFrameworkDataPath, "framework_repo");

                if (!Directory.Exists(novaFrameworkDataPath))
                {
                    Directory.CreateDirectory(novaFrameworkDataPath);
                    Debug.Log($"Created directory: {novaFrameworkDataPath}");
                }

                if (!Directory.Exists(frameworkRepoPath))
                {
                    Directory.CreateDirectory(frameworkRepoPath);
                    Debug.Log($"Created directory: {frameworkRepoPath}");
                }

                Debug.Log("准备下载框架包...");
                
                // 依次下载并安装包
                DownloadAndInstallPackagesSequentially(_gitUrlDic.ToList(), 0);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error during installation: {e.Message}\n{e.StackTrace}");
            }
        }

        // 顺序下载和安装包
        static void DownloadAndInstallPackagesSequentially(List<KeyValuePair<string, string>> gitUrls, int index)
        {
            if (index >= gitUrls.Count)
            {
                // 所有包都已安装完成，启动AutoInstallManager
                Debug.Log("启动Installer...");
                EditorApplication.delayCall += StartAutoInstallManager;
                return;
            }
            

            

            
            var currentPair = gitUrls[index];
            string packageName = currentPair.Key;
            string gitUrl = currentPair.Value;

            Debug.Log($"正在安装中 {packageName}...");

            DownloadPackageFromGit(packageName, gitUrl, Path.Combine(Path.GetDirectoryName(Application.dataPath), "NovaFrameworkData", "framework_repo"), () =>
            {
                Debug.Log($"  完成: {packageName}");
                
                // 延迟执行下一个包的安装
                EditorApplication.delayCall += () =>
                {
                    DownloadAndInstallPackagesSequentially(gitUrls, index + 1);
                };
            });
        }

        static void DownloadPackageFromGit(string packageName, string gitUrl, string targetPath, System.Action onComplete)
        {
            try
            {
                string packagePath = Path.Combine(targetPath, packageName);
                
                // 检查目标目录是否已存在
                if (Directory.Exists(packagePath))
                {
                    Debug.Log($"Package directory already exists: {packagePath}, checking if it's a git repository...");
                    
                    // 尝试使用git pull更新现有目录
                    bool updateSuccess = UpdatePackageWithGitPull(packagePath, packageName);
                    
                    if (updateSuccess)
                    {
                        Debug.Log($"Successfully updated package with git pull: {packageName}");
                        
                        // 更新成功，直接调用 onComplete 回调
                        ModifyManifestJson(packageName, onComplete);
                        return; // 提前返回，不再执行后面的克隆逻辑
                    }
                    
                    // 如果更新失败，继续执行后面的克隆逻辑
                }
                
                // 使用 Git 命令行下载包
                string command = $"git clone \"{gitUrl}\" \"{packagePath}\"";
                string workingDir = targetPath;
                
                int exitCode = ExecuteGitCommand(command, workingDir, out string output, out string error);
                
                if (exitCode == 0)
                {
                    Debug.Log($"Successfully downloaded package from {gitUrl}");
                    
                    // 修改 manifest.json
                    ModifyManifestJson(packageName, onComplete);
                }
                else
                {
                    string errorMsg = $"Failed to download package: {error}";
                    Debug.LogError(errorMsg);
                    onComplete?.Invoke(); // 确保即使失败也能继续
                }
            }
            catch (Exception e)
            {
                // 遇到异常时，记录警告而不是错误，并继续执行
                string warningMsg = $"Warning: Could not download package {packageName}, skipping. Reason: {e.Message}";
                Debug.LogWarning(warningMsg);
                
                // 跳过当前包，继续执行后续步骤
                onComplete?.Invoke();
            }
        }
        
        // 尝试使用git pull更新现有包
        private static bool UpdatePackageWithGitPull(string packagePath, string packageName)
        {
            // 检查是否是git仓库
            string gitDirPath = Path.Combine(packagePath, ".git");
            if (Directory.Exists(gitDirPath))
            {
                // 这是一个git仓库，尝试pull更新
                try
                {
                    string pullCommand = $"cd /d \"{packagePath}\" && git pull origin main";
                    
                    int pullExitCode = ExecuteGitCommand(pullCommand, ".", out string output, out string pullError);
                    
                    if (pullExitCode == 0)
                    {
                        Debug.Log($"Successfully updated package with git pull: {packageName}");
                        return true; // 更新成功
                    }
                    else
                    {
                        Debug.LogWarning($"Git pull failed for {packageName}: {pullError}");
                    }
                }
                catch (Exception pullEx)
                {
                    Debug.LogWarning($"Exception during git pull for {packageName}: {pullEx.Message}");
                }
            }
            else
            {
                Debug.Log($"{packageName} is not a git repository, will re-clone");
            }
            
            // 不管是更新失败还是非git仓库，都需要删除目录重新克隆
            // 让操作系统有机会释放文件锁
            // 不再使用Thread.Sleep，而是直接继续执行
            
            try
            {
                Directory.Delete(packagePath, true); // 递归删除目录
                Debug.Log($"Removed existing package directory: {packagePath}");
            }
            catch (UnauthorizedAccessException uaEx)
            {
                Debug.LogWarning($"Unauthorized access error: {uaEx.Message}");
                Debug.LogWarning($"Access denied to directory {packagePath}: {uaEx.Message}");
                
                // 尝试以更安全的方式删除
                try
                {
                    // 重新尝试删除
                    Directory.Delete(packagePath, true);
                    Debug.Log($"Successfully removed directory after retry: {packagePath}");
                }
                catch
                {
                    // 如果还是失败，尝试清空目录内容而不是删除目录本身
                    ClearDirectoryContents(packagePath);
                }
            }
            catch (Exception deleteEx)
            {
                Debug.LogWarning($"Failed to remove existing directory: {deleteEx.Message}");
                Debug.LogWarning($"Could not remove existing directory {packagePath}: {deleteEx.Message}");
                
                // 尝试清空目录内容而不是删除目录本身
                ClearDirectoryContents(packagePath);
            }
            
            return false; // 更新失败，需要重新克隆
        }
        
        // 辅助方法：清空目录内容
        private static void ClearDirectoryContents(string directoryPath)
        {
            var files = Directory.GetFiles(directoryPath);
            var dirs = Directory.GetDirectories(directoryPath);
            
            foreach (var file in files)
            {
                try
                {
                    File.SetAttributes(file, FileAttributes.Normal); // 移除只读属性
                    File.Delete(file);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Could not delete file {file}: {ex.Message}");
                    // 可能文件被占用，跳过等待直接尝试删除
                    try
                    {
                        File.Delete(file);
                    }
                    catch
                    {
                        Debug.LogError($"Failed to delete file after retry: {file}");
                    }
                }
            }
            
            foreach (var dir in dirs)
            {
                Directory.Delete(dir, true);
            }
        }
                
        // 执行Git命令
        private static int ExecuteGitCommand(string command, string workingDir, out string output, out string error)
        {
            output = "";
            error = "";
                    
            try
            {
                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {command}",
                    WorkingDirectory = workingDir,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                        
                using (System.Diagnostics.Process process = System.Diagnostics.Process.Start(startInfo))
                {
                    output = process.StandardOutput.ReadToEnd();
                    error = process.StandardError.ReadToEnd();
                    process.WaitForExit();
                    return process.ExitCode;
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return -1; // 表示异常
            }
        }
                
        static void ModifyManifestJson(string packageName, System.Action onComplete)
        {
            try
            {
                string manifestPath = Path.Combine(Directory.GetParent(Application.dataPath).ToString(), "Packages", "manifest.json");

                if (!File.Exists(manifestPath))
                {
                    string errorMsg = $"manifest.json not found at: {manifestPath}";
                    Debug.LogError(errorMsg);
                    onComplete?.Invoke();
                    return;
                }

                // 读取现有的 manifest.json
                string jsonContent = File.ReadAllText(manifestPath);

                // 简单的字符串操作来添加依赖项
                // 找到 "dependencies" 部分并添加新的条目
                int dependenciesStart = jsonContent.IndexOf("\"dependencies\"");
                if (dependenciesStart != -1)
                {
                    int openingBrace = jsonContent.IndexOf('{', dependenciesStart);
                    if (openingBrace != -1)
                    {
                        // 检查是否已存在相同的依赖项，避免重复添加
                        if (jsonContent.Substring(dependenciesStart, openingBrace - dependenciesStart + 200).Contains(packageName))
                        {
                            Debug.Log("Package dependency already exists in manifest.json");
                            onComplete?.Invoke();
                            return;
                        }

                        int insertPosition = jsonContent.IndexOf('\n', openingBrace + 1);
                        if (insertPosition == -1) insertPosition = openingBrace + 1;

                        string newEntry = $"\n    \"{packageName}\": \"file:./../NovaFrameworkData/framework_repo/{packageName}\",";
                        string updatedJson = jsonContent.Insert(insertPosition, newEntry);

                        File.WriteAllText(manifestPath, updatedJson);
                        Debug.Log("Successfully updated manifest.json with new package dependency");

                        // 刷新 Unity 包管理器
                        AssetDatabase.Refresh();
                        
                        // 立即执行回调，Unity会在后台处理包更新
                        onComplete?.Invoke();
                    }
                }
                else
                {
                    onComplete?.Invoke();
                }
            }
            catch (Exception e)
            {
                string errorMsg = $"Failed to modify manifest.json: {e.Message}";
                Debug.LogError(errorMsg);
                onComplete?.Invoke();
            }
        }

        // 启动AutoInstallManager
        static void StartAutoInstallManager()
        {

            
            Debug.Log("正在启动AutoInstallManager...");
            
            // 刷新AssetDatabase以确保新添加的包被识别
            AssetDatabase.Refresh();
            
            // 强制刷新所有资源，确保新下载的包被识别
            AssetDatabase.ImportAsset("Assets", ImportAssetOptions.ImportRecursive);
            
            // 延迟执行，给Unity时间处理包的加载
            EditorApplication.delayCall += () =>
            {
                // 使用更可靠的方式等待包管理器完成更新
                ResolvePackageManagerAndLoadAssembly();
            };
        }

        private static void OnPackagesRegistered(PackageRegistrationEventArgs args)
        {
            // 移除事件监听器以避免重复调用
            Events.registeringPackages -= OnPackagesRegistered;
            
            // args 包含了添加、移除或更新的包信息
            // 这里可以添加过滤条件，例如判断是否是因Resolve触发
            if (args.added.Count > 0 || args.removed.Count > 0)
            {
                Debug.Log("包解析操作已完成，包列表已更新。");
                // 在这里执行你的后续代码，例如刷新UI、初始化等
                // 使用异步方式延迟执行，避免阻塞主线程
                EditorApplication.delayCall += () =>
                {
                    TryLoadAndStartAutoInstallManager();
                };
            }
        }
      
        
        // 等待包管理器完成更新并加载程序集
        static void ResolvePackageManagerAndLoadAssembly()
        {
            // 首先强制解析包管理器
            // 注意：Client.Resolve()是一个异步操作，它本身不返回请求对象
            Events.registeringPackages += OnPackagesRegistered;
            Debug.Log("启动Client.Resolve()以解析包...");
            Client.Resolve();
            
            Debug.Log("已调用Client.Resolve()，等待包解析完成...");
        }
        
        // 尝试加载并启动AutoInstallManager
        static void TryLoadAndStartAutoInstallManager()
        {
            Debug.Log("开始尝试加载并启动AutoInstallManager...");
            
            // 再次刷新确保所有变更都已应用
            AssetDatabase.Refresh();
            
            // 使用Unity的异步机制等待程序集编译完成
            EditorApplication.delayCall += () =>
            {
                // 稍后继续执行，给Unity时间处理
                ContinueTryLoadAndStartAutoInstallManager();
            };
        }
        
        // 继续加载并启动AutoInstallManager
        private static void ContinueTryLoadAndStartAutoInstallManager()
        {
            try
            {
                // 尝试通过反射调用AutoInstallManager的StartAutoInstall方法
                Type autoInstallManagerType = null;
                
                // 首先尝试直接获取类型
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        // 排除系统程序集，只检查用户相关的程序集
                        if (assembly.FullName.StartsWith("Nova") || 
                            assembly.FullName.StartsWith("Assembly-CSharp") || 
                            assembly.FullName.Contains("Installer"))
                        {
                            var type = assembly.GetType("NovaFramework.Editor.Installer.AutoInstallManager");
                            if (type != null)
                            {
                                autoInstallManagerType = type;
                                Debug.Log($"找到AutoInstallManager类型在程序集: {assembly.FullName}");
                                break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        string exMsg = ex.Message;
                        Debug.LogWarning($"Assembly loading error (ignored): {exMsg}");
                        continue;
                    }
                }
                
                if (autoInstallManagerType != null)
                {
                    // 现在调用StartAutoInstall方法
                    var startAutoInstallMethod = autoInstallManagerType.GetMethod("StartAutoInstall", 
                        BindingFlags.Static | BindingFlags.Public);
                    
                    if (startAutoInstallMethod != null)
                    {
                        try
                        {
                            Debug.Log("正在调用AutoInstallManager.StartAutoInstall方法...");
                            
                            // 在调用StartAutoInstall之前，确保包管理器已完全解析
                            // 这里使用异步方式调用，避免阻塞
                            EditorApplication.delayCall += () =>
                            {
                                try
                                {
                                    startAutoInstallMethod.Invoke(null, null);
                                    Debug.Log("AutoInstallManager已启动，进度将由AutoInstallManager自己管理...");
                                }
                                catch (Exception invokeEx)
                                {
                                    string errorMsg = $"Error invoking AutoInstallManager.StartAutoInstall: {invokeEx.Message}";
                                    Debug.LogError(errorMsg);
                                    Debug.LogError(invokeEx.StackTrace);
                                }
                            };
                        }
                        catch (Exception ex)
                        {
                            string errorMsg = $"Error invoking AutoInstallManager.StartAutoInstall: {ex.Message}";
                            Debug.LogError(errorMsg);
                            Debug.LogError(ex.StackTrace);
                        }
                    }
                    else
                    {
                        string errorMsg = "AutoInstallManager.StartAutoInstall method not found";
                        Debug.LogError(errorMsg);
                    }
                }
                else
                {
                    // 如果找不到类型，说明安装未完成
                    Debug.LogError("无法找到AutoInstallManager，请检查安装过程是否成功完成。");
                }
            }
            catch (Exception ex)
            {
                string errorMsg = $"Unexpected error in TryLoadAndStartAutoInstallManager: {ex.Message}";
                Debug.LogError(errorMsg);
                Debug.LogError(ex.StackTrace);
            }
        }
        

        
        private static void RemoveSelf()
        {
            try
            {
                // 使用 PackageManager 移除自身
                Client.Remove(_launcherPackageName);
                Debug.Log($"Successfully removed self: {_launcherPackageName}");
                                
                // 完成安装后，让AutoInstallManager处理后续流程（打开场景、配置中心等）
                

            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to remove self: {e.Message}");
            }
        }
        
        // 公共方法供其他类调用删除launcher包
        public static void RemoveLauncherPackage()
        {
            RemoveSelf();
        }


        public static bool IsAssemblyExists(string assemblyName)
        {
            // 获取当前应用程序域中的所有已加载程序集
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            
            // 检查是否存在指定名称的程序集
            return assemblies.Any(assembly => 
                string.Equals(assembly.GetName().Name, assemblyName, StringComparison.OrdinalIgnoreCase));
        }
    
        /// <summary>
        /// 检查是否已加载必要的程序集
        /// </summary>
        /// <returns></returns>
        private static bool HasNecessaryAssemblies()
        {
            return IsAssemblyExists("NovaEditor.Installer") || IsAssemblyExists("NovaEditor.Common");
        }
    
        // 添加菜单项，允许用户手动启动自动安装
        [MenuItem("Tools/NovaFramework/启动框架自动安装 _F7", false, 1)]
        public static void ManualStartInstallation()
        {
            // 检查Nova.Installer.Editor程序集是否存在
            if (HasNecessaryAssemblies())
            {
                Debug.Log("Required assemblies already exist, attempting to start AutoInstallManager...");
                
                // 延迟执行，确保编辑器完全加载
                EditorApplication.delayCall += () =>
                {
                    TryLoadAndStartAutoInstallManager();
                };
            }
            else
            {
                Debug.Log("Required assemblies not found, starting installation process...");
                
                // 开始新的安装流程
                ExecuteInstallation();
            }
        }
    }
}
