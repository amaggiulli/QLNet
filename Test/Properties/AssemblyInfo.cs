using System.Reflection;
using System.Runtime.InteropServices;

#if ! QL_DOTNET_FRAMEWORK
using Xunit;
[assembly: CollectionBehavior(DisableTestParallelization = true)]
#endif

// Le informazioni generali relative a un assembly sono controllate dal seguente 
// insieme di attributi. Per modificare le informazioni associate a un assembly
// è necessario modificare i valori di questi attributi.
[assembly: AssemblyTitle("Test")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("Test")]
[assembly: AssemblyCopyright( "Copyright (c) 2008-2017 Andrea Maggiulli (a.maggiulli@gmail.com)" )]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Se si imposta ComVisible su false, i tipi in questo assembly non saranno visibili 
// ai componenti COM. Se è necessario accedere a un tipo in questo assembly da 
// COM, impostare su true l'attributo ComVisible per tale tipo.
[assembly: ComVisible(false)]

// Se il progetto viene esposto a COM, il GUID che segue verrà utilizzato per creare l'ID della libreria dei tipi
[assembly: Guid("846f17f1-9b43-4bb9-a5b8-254209212e39")]

// Le informazioni sulla versione di un assembly sono costituite dai seguenti quattro valori:
//
//      Numero di versione principale
//      Numero di versione secondario 
//      Numero build
//      Revisione
//
// È possibile specificare tutti i valori oppure impostare i valori predefiniti per i numeri relativi alla build e alla revisione 
// utilizzando l'asterisco (*) come descritto di seguito:
[assembly: AssemblyVersion( "1.9.1.0" )]
[assembly: AssemblyFileVersion( "1.9.0.0" )]
