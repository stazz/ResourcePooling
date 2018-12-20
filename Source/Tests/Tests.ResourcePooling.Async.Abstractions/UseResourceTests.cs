/*
 * Copyright 2017 Stanislav Muhametsin. All rights Reserved.
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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ResourcePooling.Async.Abstractions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tests.ResourcePooling.Async.Abstractions
{
   [TestClass]
   public class UseResourceTests
   {
      private const Int32 TIMEOUT = 1000;

      [TestMethod, Timeout( TIMEOUT )]
      public Task UseResourceAsyncVoid()
      {
         return PerformTest( ( pool, assert ) =>
         {
            Task Callback( Object ignored )
            {
               assert();
               return Task.CompletedTask;
            }

            return pool.UseResourceAsync( Callback, default );
         } );
      }

      [TestMethod, Timeout( TIMEOUT )]
      public Task UseResourceAsyncNonVoid()
      {
         return PerformTest( ( pool, assert ) =>
         {
            Task<Int32> Callback( Object ignored )
            {
               assert();
               return Task.FromResult( 0 );
            }

            return pool.UseResourceAsync( Callback, default );
         } );
      }

      [TestMethod, Timeout( TIMEOUT )]
      public Task UseResourceSyncVoid()
      {
         return PerformTest( ( pool, assert ) =>
         {
            void Callback( Object ignored )
            {
               assert();
            }

            return pool.UseResourceAsync( Callback, default );
         } );
      }

      [TestMethod, Timeout( TIMEOUT )]
      public Task UseResourceSyncNonVoid()
      {
         return PerformTest( ( pool, assert ) =>
         {
            Int32 Callback( Object ignored )
            {
               assert();
               return 0;
            }

            return pool.UseResourceAsync( Callback, default );
         } );
      }

      private static async Task PerformTest(
         Func<AsyncResourcePool<Object>, Action, Task> callback
         )
      {
         var pool = new TestAsyncResourcePool();
         var callbackCalled = 0;
         await callback( pool, () =>
         {
            Assert.AreEqual(
                  0,
                  Interlocked.CompareExchange( ref callbackCalled, 1, 0 )
                  );
            Assert.IsTrue( pool.AwaitCalled );
            Assert.IsFalse( pool.DisposeCalled );
         } );

         Assert.AreNotEqual( 0, callbackCalled );
         Assert.IsTrue( pool.AwaitCalled );
         Assert.IsTrue( pool.DisposeCalled );
      }

   }

   internal sealed class TestAsyncResourcePool : AsyncResourcePool<Object>
   {
      private Int32 _awaitCalled;
      private Int32 _disposeCalled;

      public AsyncResourceUsage<Object> GetResourceUsage( CancellationToken token )
      {
         return new DefaultAsyncResourceUsage<Object>(
            token,
            () =>
            {
               Assert.AreEqual(
                  0,
                  Interlocked.CompareExchange( ref this._awaitCalled, 1, 0 )
                  );
               return Task.FromResult( new Object() );
            },
            () =>
            {
               Assert.AreNotEqual( 0, this._awaitCalled );
               Assert.AreEqual(
                  0,
                  Interlocked.CompareExchange( ref this._disposeCalled, 1, 0 )
                  );
               return Task.CompletedTask;
            }
            );
      }

      public void ResetFactoryState()
      {

      }


      public Boolean AwaitCalled => this._awaitCalled != 0;

      public Boolean DisposeCalled => this._disposeCalled != 0;
   }

}
