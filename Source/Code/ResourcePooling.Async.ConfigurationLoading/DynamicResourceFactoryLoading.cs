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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using ResourcePooling.Async.Abstractions;
using ResourcePooling.Async.ConfigurationLoading;
using System;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UtilPack;
using UtilPack.JSON.Configuration;

namespace ResourcePooling.Async.ConfigurationLoading
{
   /// <summary>
   /// This interface specializes <see cref="ResourceFactoryDynamicCreationConfiguration"/> for situations when the resource factory parameters are serialized into a file in such way that they are deserializable using Microsoft.Extensions.Configuration utilities.
   /// Use <see cref="E_ResourcePooling.CreateAsyncResourceFactoryUsingConfiguration"/> to create async resource factory from this <see cref="ResourceFactoryDynamicCreationFileBasedConfiguration"/>.
   /// </summary>
   public interface ResourceFactoryDynamicCreationFileBasedConfiguration : ResourceFactoryDynamicCreationConfiguration
   {
      /// <summary>
      /// Gets or sets the path to the configuration file holding creation parameter for <see cref="AsyncResourceFactoryProvider.BindCreationParameters"/>.
      /// </summary>
      /// <value>The path to the configuration file holding creation parameter for <see cref="AsyncResourceFactoryProvider.BindCreationParameters"/>.</value>
      /// <remarks>
      /// This property should not be used together with <see cref="PoolConfigurationFileContents"/>, because <see cref="PoolConfigurationFileContents"/> takes precedence over this property.
      /// </remarks>
      /// <seealso cref="PoolConfigurationFileContents"/>
      String PoolConfigurationFilePath { get; }

      /// <summary>
      /// Gets or sets the configuration file contents in-place, instead of using <see cref="PoolConfigurationFilePath"/> file path.
      /// This property takes precedence over <see cref="PoolConfigurationFilePath"/>
      /// </summary>
      /// <seealso cref="PoolConfigurationFilePath"/>
      String PoolConfigurationFileContents { get; }
   }

   /// <summary>
   /// This is default implementation of <see cref="ResourceFactoryDynamicCreationFileBasedConfiguration"/> with setters, so that it can be used with Microsoft.Extensions.Configuration packages.
   /// </summary>
   public class DefaultResourceFactoryDynamicCreationFileBasedConfiguration : DefaultResourceFactoryDynamicCreationConfiguration, ResourceFactoryDynamicCreationFileBasedConfiguration
   {
      /// <inheritdoc />
      public String PoolConfigurationFilePath { get; set; }

      /// <inheritdoc />
      public String PoolConfigurationFileContents { get; set; }
   }

   /// <summary>
   /// This class contains the defaults used for <see cref="E_ResourcePooling.CreateAsyncResourceFactoryUsingConfiguration"/>.
   /// </summary>
   public static class Defaults
   {
      /// <summary>
      /// This method creates a callback to first check <see cref="ResourceFactoryDynamicCreationFileBasedConfiguration.PoolConfigurationFileContents"/> and then <see cref="ResourceFactoryDynamicCreationFileBasedConfiguration.PoolConfigurationFilePath"/> in order to load the file as JSON.
      /// This JSON is then used to create a configuration object of type <see cref="AsyncResourceFactoryProvider.DataTypeForCreationParameter"/>.
      /// </summary>
      /// <param name="configuration">The <see cref="ResourceFactoryDynamicCreationFileBasedConfiguration"/>.</param>
      /// <returns>A callback which will create configuration object based on <paramref name="configuration"/> and given <see cref="AsyncResourceFactoryProvider"/>.</returns>
      /// <remarks>
      /// The created callback with throw <see cref="InvalidOperationException"/> if both <see cref="ResourceFactoryDynamicCreationFileBasedConfiguration.PoolConfigurationFileContents"/> and <see cref="ResourceFactoryDynamicCreationFileBasedConfiguration.PoolConfigurationFilePath"/> are <c>null</c> or empty.
      /// </remarks>
      /// <exception cref="ArgumentNullException">If <paramref name="configuration"/> is <c>null</c>.</exception>
      public static Func<AsyncResourceFactoryProvider, Object> CreateDefaultCreationParametersProvider(
         ResourceFactoryDynamicCreationFileBasedConfiguration configuration
         )
      {
         ArgumentValidator.ValidateNotNull( nameof( configuration ), configuration );
         return factoryProvider =>
         {
            var contents = configuration.PoolConfigurationFileContents;
            var builder = new ConfigurationBuilder();
            if ( !String.IsNullOrEmpty( contents ) )
            {
               builder.AddJsonContents( contents );
            }
            else
            {
               var path = configuration.PoolConfigurationFilePath;
               if ( String.IsNullOrEmpty( path ) )
               {
                  throw new InvalidOperationException( "Configuration file path was not provided." );
               }
               else
               {
                  builder.AddJsonFile( System.IO.Path.GetFullPath( path ) );
               }
            }


            return builder
               .Build()
               .Get( factoryProvider.DataTypeForCreationParameter );
         };
      }
   }
}

/// <summary>
/// This class contains extension methods for types defined in this assembly.
/// </summary>
public static partial class E_ResourcePooling
{
   /// <summary>
   /// This method will asynchronously load the <see cref="AsyncResourceFactory{TResource}"/> based on information in this <see cref="ResourceFactoryDynamicCreationFileBasedConfiguration"/>.
   /// </summary>
   /// <typeparam name="TResource">The type of the resources provided by returned <see cref="AsyncResourceFactory{TResource}"/>.</typeparam>
   /// <param name="configuration">This <see cref="ResourceFactoryDynamicCreationFileBasedConfiguration"/>.</param>
   /// <param name="assemblyLoader">The callback to asynchronously load assembly. The parameters are, in this order: package ID, package version, and path within the package.</param>
   /// <param name="token">The <see cref="CancellationToken"/> for this asynchronous operation.</param>
   /// <param name="creationParametersProvider">The optional callback to create creation parameters to bind the returned <see cref="AsyncResourceFactory{TResource}"/> to. Will be result of <see cref="Defaults.CreateDefaultCreationParametersProvider"/> if not provided here.</param>
   /// <returns>Asynchronously returns instance of <see cref="AsyncResourceFactory{TResource}"/>, or throws an exception.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="ResourceFactoryDynamicCreationConfiguration"/> is <c>null.</c></exception>
   /// <exception cref="InvalidOperationException">If for some reason the <see cref="AsyncResourceFactory{TResource}"/> could not be loaded.</exception>
   public static Task<AsyncResourceFactory<TResource>> CreateAsyncResourceFactoryUsingConfiguration<TResource>(
      this ResourceFactoryDynamicCreationFileBasedConfiguration configuration,
      Func<String, String, String, CancellationToken, Task<Assembly>> assemblyLoader,
      CancellationToken token,
      Func<AsyncResourceFactoryProvider, Object> creationParametersProvider = null
      )
   {
      return configuration.CreateAsyncResourceFactory<TResource>(
         assemblyLoader,
         creationParametersProvider ?? Defaults.CreateDefaultCreationParametersProvider( configuration ),
         token
         );
   }
}