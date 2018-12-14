[![Build status](https://ci.appveyor.com/api/projects/status/pbbaew65p059kn9u/branch/develop?svg=true)](https://ci.appveyor.com/project/stazz/resourcepooling/branch/develop)

# ResourcePooling

This project contains libraries with APIs and implementations for pools of asynchronous resource.
The resource type is freely parametrizable via interface type parameter, which also has `out` variance.

Various kinds of pools (non-caching, upper-bound-limited, timeouting, etc) can be created via extension methods to `AsyncResourceFactory<TResource>` interface.
The extension methods are contained in [ResourcePooling.Async.Implementation](Source/Code/ResourcePooling.Async.Implementation) project.

The `AsyncResourceFactory<TResource>` interface itself typically created by gaining access to `AsyncResourceFactoryProvider` instance and invoking `BindCreationParameters` method; or by dynamically via extension method of `ResourceFactoryDynamicCreationConfiguration`.