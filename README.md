# StreamlineMVVM
* MVVM Framework comes with:
  * Logging Utility
  * Config File Handling
  * INI File Handling
  * Minor System.IO wrapping
  * Specialized Regex Match/Replace
  * HTTP/SMTP Wrapper
  * Task Worker mimic of older .net background worker.
* Also can add custom dialogs by creating a User Control and using that as the DataTemplate for window content.
* Two versions available for compatibility with 2 different .net distributions.
  * .net Framework 4.0
  * .net 5.0

# Injection
* Add Reference: StreamlineMVVM.dll
* Add Application Resource:
  * `<ResourceDictionary Source="pack://application:,,,/StreamlineMVVM;component/Templates/MergedResources.xaml"/>`
  * Add to XAML where resources are used:`xmlns:ext="clr-namespace:MVVMFramework;assembly=MVVMFramework"`
* Supports Embedding with this code: https://github.com/pvpxan/DLLEmbedding

# Framework
* See https://github.com/pvpxan/MVVMTemplate for example code of how to use this framework.
* Creation of a ViewModel should be done by extending your class with `ViewModelBase`.
* The `RelayCommand` Class is used for tieing your business logic to a bound `ICommand`.

# Classes
* `LogWriter` - The Logging classes are simple thread safe log writers with multiple options.
  * `bool SetPath(string path, string user, string application)` (Needed to assign where you want the log files to go.)
  * `Exception(string log, Exception ex)`
  * `LogEntry(string log)`
* `LogWriterWPF`
  * `LogDisplay(string log, MessageBoxImage messageType)`
  * `LogDisplay(string log, MessageBoxImage messageBoxImage, Window window)`
  * `ExceptionDisplay(string log, Exception ex, bool showFull)`
  * `ExceptionDisplay(string log, Exception ex, bool showFull, Window window)`
* `Config` - Writes to `app.config`.
  * `string Read(string key)` Used for reading `app.config` file.
  * `bool Update(string key, string value)` Writes to `app.config` file.
* `INI` - Writes to INI files.
  * `bool? ReadBool(string file, string key)`
  * `int? ReadInt(string file, string key)`
  * `string Read(string file, string key)`
  * `bool Write(string file, string key, string value, bool create, bool backup)`
* `SystemIO` - Simple IO wrapping.
  * `PathType GetPathType(string path)`
  * `bool Delete(string file)`
  * `bool Copy(string fileSource, string fileTarget, bool overwrite)`
  * `bool CreateDirectory(string directory)`
  * `OutputResult[] CopyDirectory(string sourceDirectory, string targetDirectory)`
* `RegexFunctions`
  * Lots of matching and replacing based on number, special characters, and spacing.
* `HTTP`/`SMTP` - Wrapped use of `System.Net` and `WebRequest`
* `TaskWorker` - Event driven use of `System.Threading.Tasks` to mimic behavior of older `BackgroundWorker` class.
