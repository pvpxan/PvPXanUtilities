# StreamlineMVVM
* MVVM Framework comes with:
  * Enhanced Windows MessageBox
  * Logging Utility
  * Config File Handling
  * INI File Handling
  * Minor System.IO wrapping
  * Specialized Regex Match/Replace
* Also can add custom dialogs by creating a User Control and using that as the DataTemplate for window content.
* Two versions available for compatibility with 2 different .net Framework distributions.
  * .net Framework 4.0
  * .net Framework 4.7.2

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

# MessageBoxEnhanced
* Customizable replacement for the standard C# MessageBox with more options for buttons and dialog results.
* __NOTE:__ 
  * Using this on application launch BEFORE you open another window will result in the Application class shutting down.
  * Set `Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;` to get around this.
  * Downside is you have to call an explicit shutdown or change the setting back.

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
* `FactoryService`
  * Pretty new addition with tools to help create robust Window Factory classes.
  
# Extention Methods
`ComboBoxHighlight`\
`CornerRadius`\
`FocusThickness`\
`MouseOverBackground`\
`MouseOverBorder`\
`MouseDownBackground`\
`MouseDownBorder`\
`SelectionActiveBackground`\
`SelectionActiveBorder`\
`SelectionInactiveBackground`\
`SelectionInactiveBorder`\
`CommandParameter`\
`Command`

# Styles/Templates
* Some of these are a semi work in progress, but all work well when you know how to use them.
`pack://application:,,,/StreamlineMVVM;component/Templates/ButtonExtended.xaml`\
`pack://application:,,,/StreamlineMVVM;component/Templates/CheckBoxExtended.xaml`\
`pack://application:,,,/StreamlineMVVM;component/Templates/FlatComboBox.xaml`\
`pack://application:,,,/StreamlineMVVM;component/Templates/GroupBoxImproved.xaml`\
`pack://application:,,,/StreamlineMVVM;component/Templates/ListBoxExtended.xaml`\
`pack://application:,,,/StreamlineMVVM;component/Templates/ListViewExtended.xaml`\
`pack://application:,,,/StreamlineMVVM;component/Templates/ScrollBarExtended.xaml`
`pack://application:,,,/StreamlineMVVM;component/Templates/TextBoxExtended.xaml`\
`pack://application:,,,/StreamlineMVVM;component/Templates/TreeViewExtended.xaml`\

# DialogBaseWindow
* Generic Window with a bound content presenter.
* Creating custom dialogs requires use of `DialogViewModel` to extend a datacontext class of your own making.
