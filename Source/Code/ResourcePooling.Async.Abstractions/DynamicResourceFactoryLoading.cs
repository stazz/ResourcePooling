/*
 * Copyright 2018 Stanislav Muhametsin. All rights Reserved.
 *
 * Licensed  under the  Apache License,  Version 2.0  (the "License");
 * you may not use  this file  except in  compliance with the License.
 * You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed  under the  License is distributed on an "AS IS" BASIS,
 * WITHOUT  WARRANTIES OR CONDITIONS  OF ANY KIND, either  express  or
 * implied.
 *
 * See the License for the specific language governing permissions and
 * limitations under the License. 
 */
using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using UtilPack;
using ResourcePooling.Async.Abstractions;

using TTypeInfo = System.
#if NET40
   Type
#else
   Reflection.TypeInfo
#endif
   ;

namespace ResourcePooling.Async.Abstractions
{
   /// <summary>
   /// This is configuration interface used when dynamically loading an instance of <see cref="AsyncResourceFactory{TResource}"/>, typically with <see cref="E_ResourcePooling.CreateAsyncResourceFactory"/> extension method for this type.
   /// </summary>
   public interface ResourceFactoryDynamicCreationConfiguration
   {
      /// <summary>
      /// Gets or sets the value for NuGet package ID of the package holding the type implementing <see cref="AsyncResourceFactoryProvider"/>.
      /// </summary>
      /// <value>The value for NuGet package ID of the package holding the type implementing <see cref="AsyncResourceFactoryProvider"/>.</value>
      /// <seealso cref="PoolProviderVersion"/>
      String PoolProviderPackageID { get; }

      /// <summary>
      /// Gets or sets the value for NuGet package version of the package holding the type implementing <see cref="AsyncResourceFactoryProvider"/>.
      /// </summary>
      /// <value>The value for NuGet package version of the package holding the type implementing <see cref="AsyncResourceFactoryProvider"/>.</value>
      /// <remarks>
      /// The value, if specified, should be parseable into <see cref="T:NuGet.Versioning.VersionRange"/>.
      /// If left out, then the newest version will be used, but this will cause additional overhead when querying for the newest version.
      /// </remarks>
      String PoolProviderVersion { get; }

      /// <summary>
      /// Gets or sets the path within NuGet package specified by <see cref="PoolProviderPackageID"/> and <see cref="PoolProviderVersion"/> properties where the assembly holding type implementing <see cref="AsyncResourceFactoryProvider"/> resides.
      /// </summary>
      /// <value>The path within NuGet package specified by <see cref="PoolProviderPackageID"/> and <see cref="PoolProviderVersion"/> properties where the assembly holding type implementing <see cref="AsyncResourceFactoryProvider"/> resides.</value>
      /// <remarks>
      /// This property will be used only for NuGet packages with more than assembly within its framework-specific folder.
      /// </remarks>
      String PoolProviderAssemblyPath { get; }

      /// <summary>
      /// Gets or sets the name of the type implementing <see cref="AsyncResourceFactoryProvider"/>, located in assembly within NuGet package specified by <see cref="PoolProviderPackageID"/> and <see cref="PoolProviderVersion"/> properties.
      /// </summary>
      /// <value>The name of the type implementing <see cref="AsyncResourceFactoryProvider"/>, located in assembly within NuGet package specified by <see cref="PoolProviderPackageID"/> and <see cref="PoolProviderVersion"/> properties.</value>
      /// <remarks>
      /// This value can be left out so that all types of the assembly will be searched to check if any implements <see cref="AsyncResourceFactoryProvider"/>, and use the first suitable type.
      /// </remarks>
      String PoolProviderTypeName { get; }


   }

   /// <summary>
   /// This is default implementation of <see cref="ResourceFactoryDynamicCreationConfiguration"/> with setters, so that it can be used with Microsoft.Extensions.Configuration packages.
   /// </summary>
   public class DefaultResourceFactoryDynamicCreationConfiguration : ResourceFactoryDynamicCreationConfiguration
   {
      /// <inheritdoc />
      public String PoolProviderPackageID { get; set; }

      /// <inheritdoc />
      public String PoolProviderVersion { get; set; }

      /// <inheritdoc />
      public String PoolProviderAssemblyPath { get; set; }

      /// <inheritdoc />
      public String PoolProviderTypeName { get; set; }
   }
}

/// <summary>
/// This class contains extension methods for types defined in this assembly.
/// </summary>
public static partial class E_ResourcePooling
{
   /// <summary>
   /// This method asynchronously loads an instance of <see cref="AsyncResourceFactory{TResource}"/> using this <see cref="ResourceFactoryDynamicCreationConfiguration"/> along with required callbacks.
   /// </summary>
   /// <typeparam name="TResource">The type of resource that returned <see cref="AsyncResourceFactory{TResource}"/> provides.</typeparam>
   /// <param name="configuration">This <see cref="ResourceFactoryDynamicCreationConfiguration"/>.</param>
   /// <param name="assemblyLoader">The callback to asynchronously load assembly. The parameters are, in this order: package ID, package version, and path within the package.</param>
   /// <param name="creationParametersProvider">The callback to create creation parameters to bind the returned <see cref="AsyncResourceFactory{TResource}"/> to.</param>
   /// <param name="token">The <see cref="CancellationToken"/> for this asynchronous operation.</param>
   /// <returns>Asynchronously returns instance of <see cref="AsyncResourceFactory{TResource}"/>, or throws an exception.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="ResourceFactoryDynamicCreationConfiguration"/> is <c>null.</c></exception>
   /// <exception cref="InvalidOperationException">If for some reason the <see cref="AsyncResourceFactory{TResource}"/> could not be loaded.</exception>
   /// <seealso cref="AcquireResourcePoolProvider"/>
   public static async Task<AsyncResourceFactory<TResource>> CreateAsyncResourceFactory<TResource>(
      this ResourceFactoryDynamicCreationConfiguration configuration,
      Func<String, String, String, CancellationToken, Task<Assembly>> assemblyLoader,
      Func<AsyncResourceFactoryProvider, Object> creationParametersProvider,
      CancellationToken token
      )
   {
      var factory = await AcquireResourcePoolProvider( configuration, assemblyLoader, token );
      var value = factory.GetFirstOrDefault();

      return ( value ?? throw new InvalidOperationException( factory.GetSecondOrDefault() ?? "Unspecified error." ) )
         .BindCreationParameters<TResource>( creationParametersProvider( value ) );
   }

   /// <summary>
   /// This method asynchornously loads an instance of <see cref="AsyncResourceFactoryProvider"/> using this <see cref="ResourceFactoryDynamicCreationConfiguration"/> along with assembly loader callback.
   /// </summary>
   /// <param name="configuration">This <see cref="ResourceFactoryDynamicCreationConfiguration"/>.</param>
   /// <param name="assemblyLoader">The callback to asynchronously load assembly. The parameters are, in this order: package ID, package version, and path within the package.</param>
   /// <param name="token">The <see cref="CancellationToken"/> for this asynchronous operation.</param>
   /// <returns>Asynchronously returns a value which either has <see cref="AsyncResourceFactoryProvider"/> or an error message.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="ResourceFactoryDynamicCreationConfiguration"/> is <c>null</c>.</exception>
   public static async Task<EitherOr<AsyncResourceFactoryProvider, String>> AcquireResourcePoolProvider(
      ResourceFactoryDynamicCreationConfiguration configuration,
      Func<String, String, String, CancellationToken, Task<Assembly>> assemblyLoader,
      CancellationToken token
      )
   {
      AsyncResourceFactoryProvider retVal = null;
      String errorMessage = null;
      if ( assemblyLoader != null )
      {
         var packageID = configuration.PoolProviderPackageID;
         if ( !String.IsNullOrEmpty( packageID ) )
         {
            try
            {
               var assembly = await assemblyLoader(
                  packageID, // package ID
                  configuration.PoolProviderVersion,  // optional package version
                  configuration.PoolProviderAssemblyPath, // optional assembly path within package
                  token
                  );
               if ( assembly != null )
               {
                  // Now search for the type
                  var typeName = configuration.PoolProviderTypeName;
                  var parentType = typeof( AsyncResourceFactoryProvider ).GetTypeInfo();
                  var checkParentType = !String.IsNullOrEmpty( typeName );
                  Type providerType;
                  if ( checkParentType )
                  {
                     // Instantiate directly
                     providerType = assembly.GetType( typeName ); //, false, false );
                  }
                  else
                  {
                     // Search for first available
                     providerType = assembly.
#if NET40
                           GetTypes()
#else
                           DefinedTypes
#endif
                           .FirstOrDefault( t => !t.IsInterface && !t.IsAbstract && t.IsPublic && parentType.IsAssignableFromIgnoreAssemblyVersion( t ) )
#if !NET40
                           ?.AsType()
#endif
                           ;
                  }

                  if ( providerType != null )
                  {
                     if ( !checkParentType || parentType.IsAssignableFromIgnoreAssemblyVersion( providerType.GetTypeInfo() ) )
                     {
                        // All checks passed, instantiate the pool provider
                        retVal = (AsyncResourceFactoryProvider) Activator.CreateInstance( providerType );
                     }
                     else
                     {
                        errorMessage = $"The type \"{providerType.FullName}\" in \"{assembly}\" does not have required parent type \"{parentType.FullName}\".";
                     }
                  }
                  else
                  {
                     errorMessage = $"Failed to find type within assembly in \"{assembly}\", try specify \"{nameof( configuration.PoolProviderTypeName )}\" configuration parameter.";
                  }
               }
               else
               {
                  errorMessage = $"Failed to load resource pool provider package \"{packageID}\".";
               }
            }
            catch ( Exception exc )
            {
               errorMessage = $"An exception occurred when acquiring resource pool provider: {exc.Message}";
            }
         }
         else
         {
            errorMessage = $"No NuGet package ID were provided as \"{nameof( configuration.PoolProviderPackageID )}\" configuration parameter. The package ID should be of the package holding implementation for \"{nameof( AsyncResourceFactoryProvider )}\" type.";
         }
      }
      else
      {
         errorMessage = "Task must be provided callback to load NuGet packages (just make constructor taking it as argument and use UtilPack.NuGet.MSBuild task factory).";
      }

      return retVal == null ?
         new EitherOr<AsyncResourceFactoryProvider, String>( errorMessage ) :
         new EitherOr<AsyncResourceFactoryProvider, String>( retVal );
   }

   private static Boolean IsAssignableFromIgnoreAssemblyVersion( this TTypeInfo parentType, TTypeInfo childType )
   {
      return parentType.IsAssignableFrom( childType ) || childType.AsDepthFirstEnumerable( t => t.BaseType?.GetTypeInfo().Singleton().Concat( t.
#if NET40
         GetInterfaces()
#else
         ImplementedInterfaces
#endif
         .Select( i => i.GetTypeInfo() )
            ) ).Any( t =>
            String.Equals( t.Namespace, parentType.Namespace )
            && String.Equals( t.Name, parentType.Name )
            && String.Equals( t.Assembly.GetName().Name, parentType.Assembly.GetName().Name )
            && ArrayEqualityComparer<Byte>.ArrayEquality( parentType.Assembly.GetName().GetPublicKeyToken(), t.Assembly.GetName().GetPublicKeyToken() )
            );
   }

   //private static Object ProvideResourcePoolCreationParameters(
   //   ResourcePoolDynamicCreationConfiguration configuration,
   //   AsyncResourceFactoryProvider poolProvider
   //   )
   //{
   //   var contents = configuration.PoolConfigurationFileContents;
   //   IFileProvider fileProvider;
   //   String path;
   //   if ( !String.IsNullOrEmpty( contents ) )
   //   {
   //      path = StringContentFileProvider.PATH;
   //      fileProvider = new StringContentFileProvider( contents );
   //   }
   //   else
   //   {
   //      path = configuration.PoolConfigurationFilePath;
   //      if ( String.IsNullOrEmpty( path ) )
   //      {
   //         throw new InvalidOperationException( "Configuration file path was not provided." );
   //      }
   //      else
   //      {
   //         path = System.IO.Path.GetFullPath( path );
   //         fileProvider = null; // Use defaults
   //      }
   //   }


   //   return new ValueTask<Object>( new ConfigurationBuilder()
   //      .AddJsonFile( fileProvider, path, false, false )
   //      .Build()
   //      .Get( poolProvider.DataTypeForCreationParameter ) );
   //}

   //private static AsyncResourcePool<TResource> AcquireResourcePool<TResource>(
   //   AsyncResourceFactoryProvider poolProvider,
   //   Object poolCreationArgs
   //   )
   //{
   //   return poolProvider
   //      .BindCreationParameters<TResource>( poolCreationArgs )
   //      .CreateOneTimeUseResourcePool()
   //      .WithoutExplicitAPI();
   //}
}