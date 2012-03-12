﻿using Microsoft.TestCommon;
using Moq;
using Xunit;
using Assert = Microsoft.TestCommon.AssertEx;

// There are several tests which need unreachable code (return after throw) to guarantee the correct lambda signature
#pragma warning disable 0162

namespace System.Threading.Tasks
{
    public class TaskHelpersExtensionsTest
    {
        // -----------------------------------------------------------------
        //  Task.Catch(Func<Exception, Task>)

        [Fact]
        public Task Catch_NoInputValue_CatchesException_Handled()
        {
            // Arrange
            return TaskHelpers.FromError(new InvalidOperationException())

            // Act
                              .Catch(ex =>
                              {
                                  Assert.NotNull(ex);
                                  Assert.IsType<InvalidOperationException>(ex);
                                  return TaskHelpers.Completed();
                              })

            // Assert
                              .ContinueWith(task =>
                              {
                                  Assert.Equal(TaskStatus.RanToCompletion, task.Status);
                              });
        }

        [Fact]
        public Task Catch_NoInputValue_CatchesException_Rethrow()
        {
            // Arrange
            return TaskHelpers.FromError(new InvalidOperationException())

            // Act
                              .Catch(ex =>
                              {
                                  return TaskHelpers.FromError(ex);
                              })

            // Assert
                              .ContinueWith(task =>
                              {
                                  Assert.Equal(TaskStatus.Faulted, task.Status);
                                  Assert.IsType<InvalidOperationException>(task.Exception.GetBaseException());
                              });
        }

        [Fact]
        public Task Catch_NoInputValue_ReturningNullFromCatchIsProhibited()
        {
            // Arrange
            return TaskHelpers.FromError(new Exception())

            // Act
                              .Catch(ex =>
                              {
                                  return null;
                              })

            // Assert
                              .ContinueWith(task =>
                              {
                                  Assert.Equal(TaskStatus.Faulted, task.Status);
                                  Assert.IsException<InvalidOperationException>(task.Exception, "You cannot return null from the TaskHelpersExtensions.Catch continuation. You must return a valid task or throw an exception.");
                              });
        }

        [Fact, PreserveSyncContext]
        public Task Catch_NoInputValue_CompletedTaskOfSuccess_DoesNotRunContinuationAndDoesNotSwitchContexts()
        {
            // Arrange
            bool ranContinuation = false;
            var syncContext = new Mock<SynchronizationContext> { CallBase = true };
            SynchronizationContext.SetSynchronizationContext(syncContext.Object);

            return TaskHelpers.Completed()

            // Act
                              .Catch(ex =>
                              {
                                  ranContinuation = true;
                                  return TaskHelpers.Completed();
                              })

            // Assert
                              .ContinueWith(task =>
                              {
                                  Assert.False(ranContinuation);
                                  syncContext.Verify(sc => sc.Post(It.IsAny<SendOrPostCallback>(), null), Times.Never());
                              });
        }

        [Fact, PreserveSyncContext]
        public Task Catch_NoInputValue_CompletedTaskOfCancellation_DoesNotRunContinuationAndDoesNotSwitchContexts()
        {
            // Arrange
            bool ranContinuation = false;
            var syncContext = new Mock<SynchronizationContext> { CallBase = true };
            SynchronizationContext.SetSynchronizationContext(syncContext.Object);

            return TaskHelpers.Canceled()

            // Act
                              .Catch(ex =>
                              {
                                  ranContinuation = true;
                                  return TaskHelpers.Completed();
                              })

            // Assert
                              .ContinueWith(task =>
                              {
                                  Assert.False(ranContinuation);
                                  syncContext.Verify(sc => sc.Post(It.IsAny<SendOrPostCallback>(), null), Times.Never());
                              });
        }

        [Fact, PreserveSyncContext]
        public Task Catch_NoInputValue_CompletedTaskOfFault_RunsOnSameThreadAndDoesNotPostToSynchronizationContext()
        {
            // Arrange
            int outerThreadId = Thread.CurrentThread.ManagedThreadId;
            int innerThreadId = Int32.MinValue;
            Exception thrownException = new Exception();
            Exception caughtException = null;
            var syncContext = new Mock<SynchronizationContext> { CallBase = true };
            SynchronizationContext.SetSynchronizationContext(syncContext.Object);

            return TaskHelpers.FromError(thrownException)

            // Act
                              .Catch(ex =>
                              {
                                  caughtException = ex;
                                  innerThreadId = Thread.CurrentThread.ManagedThreadId;
                                  return TaskHelpers.Completed();
                              })

            // Assert
                              .ContinueWith(task =>
                              {
                                  Assert.Same(thrownException, caughtException);
                                  Assert.Equal(innerThreadId, outerThreadId);
                                  syncContext.Verify(sc => sc.Post(It.IsAny<SendOrPostCallback>(), null), Times.Never());
                              });
        }

        [Fact, PreserveSyncContext]
        public Task Catch_NoInputValue_IncompleteTaskOfSuccess_DoesNotRunContinuationAndDoesNotSwitchContexts()
        {
            // Arrange
            bool ranContinuation = false;
            var syncContext = new Mock<SynchronizationContext> { CallBase = true };
            SynchronizationContext.SetSynchronizationContext(syncContext.Object);

            Task incompleteTask = new Task(() => { });

            // Act
            Task resultTask = incompleteTask.Catch(ex =>
            {
                ranContinuation = true;
                return TaskHelpers.Completed();
            });

            // Assert
            incompleteTask.Start();

            return resultTask.ContinueWith(task =>
            {
                Assert.False(ranContinuation);
                syncContext.Verify(sc => sc.Post(It.IsAny<SendOrPostCallback>(), null), Times.Never());
            });
        }

        [Fact, PreserveSyncContext]
        public Task Catch_NoInputValue_IncompleteTaskOfCancellation_DoesNotRunContinuationAndDoesNotSwitchContexts()
        {
            // Arrange
            bool ranContinuation = false;
            var syncContext = new Mock<SynchronizationContext> { CallBase = true };
            SynchronizationContext.SetSynchronizationContext(syncContext.Object);

            Task incompleteTask = new Task(() => { });
            Task resultTask = incompleteTask.ContinueWith(task => TaskHelpers.Canceled()).Unwrap();

            // Act
            resultTask = resultTask.Catch(ex =>
            {
                ranContinuation = true;
                return TaskHelpers.Completed();
            });

            // Assert
            incompleteTask.Start();

            return resultTask.ContinueWith(task =>
            {
                Assert.False(ranContinuation);
                syncContext.Verify(sc => sc.Post(It.IsAny<SendOrPostCallback>(), null), Times.Never());
            });
        }

        [Fact, PreserveSyncContext]
        public Task Catch_NoInputValue_IncompleteTaskOfFault_RunsOnNewThreadAndPostsToSynchronizationContext()
        {
            // Arrange
            int outerThreadId = Thread.CurrentThread.ManagedThreadId;
            int innerThreadId = Int32.MinValue;
            Exception thrownException = new Exception();
            Exception caughtException = null;
            var syncContext = new Mock<SynchronizationContext> { CallBase = true };
            SynchronizationContext.SetSynchronizationContext(syncContext.Object);

            Task incompleteTask = new Task(() => { throw thrownException; });

            // Act
            Task resultTask = incompleteTask.Catch(ex =>
            {
                caughtException = ex;
                innerThreadId = Thread.CurrentThread.ManagedThreadId;
                return TaskHelpers.Completed();
            });

            // Assert
            incompleteTask.Start();

            return resultTask.ContinueWith(task =>
            {
                Assert.Same(thrownException, caughtException);
                Assert.NotEqual(innerThreadId, outerThreadId);
                syncContext.Verify(sc => sc.Post(It.IsAny<SendOrPostCallback>(), null), Times.Once());
            });
        }

        // -----------------------------------------------------------------
        //  Task<T>.Catch(Func<Exception, Task<T>>)

        [Fact]
        public Task Catch_WithInputValue_CatchesException_Handled()
        {
            // Arrange
            return TaskHelpers.FromError<int>(new InvalidOperationException())

            // Act
                              .Catch(ex =>
                              {
                                  Assert.NotNull(ex);
                                  Assert.IsType<InvalidOperationException>(ex);
                                  return TaskHelpers.FromResult(42);
                              })

            // Assert
                              .ContinueWith(task =>
                              {
                                  Assert.Equal(TaskStatus.RanToCompletion, task.Status);
                              });
        }

        [Fact]
        public Task Catch_WithInputValue_CatchesException_Rethrow()
        {
            // Arrange
            return TaskHelpers.FromError<int>(new InvalidOperationException())

            // Act
                              .Catch(ex =>
                              {
                                  return TaskHelpers.FromError<int>(ex);
                              })

            // Assert
                              .ContinueWith(task =>
                              {
                                  Assert.Equal(TaskStatus.Faulted, task.Status);
                                  Assert.IsType<InvalidOperationException>(task.Exception.GetBaseException());
                              });
        }

        [Fact]
        public Task Catch_WithInputValue_ReturningNullFromCatchIsProhibited()
        {
            // Arrange
            return TaskHelpers.FromError<int>(new Exception())

            // Act
                              .Catch(ex =>
                              {
                                  return null;
                              })

            // Assert
                              .ContinueWith(task =>
                              {
                                  Assert.Equal(TaskStatus.Faulted, task.Status);
                                  Assert.IsException<InvalidOperationException>(task.Exception, "You cannot return null from the TaskHelpersExtensions.Catch continuation. You must return a valid task or throw an exception.");
                              });
        }

        [Fact, PreserveSyncContext]
        public Task Catch_WithInputValue_CompletedTaskOfSuccess_DoesNotRunContinuationAndDoesNotSwitchContexts()
        {
            // Arrange
            bool ranContinuation = false;
            var syncContext = new Mock<SynchronizationContext> { CallBase = true };
            SynchronizationContext.SetSynchronizationContext(syncContext.Object);

            return TaskHelpers.FromResult(21)

            // Act
                              .Catch(ex =>
                              {
                                  ranContinuation = true;
                                  return TaskHelpers.FromResult(42);
                              })

            // Assert
                              .ContinueWith(task =>
                              {
                                  Assert.False(ranContinuation);
                                  syncContext.Verify(sc => sc.Post(It.IsAny<SendOrPostCallback>(), null), Times.Never());
                              });
        }

        [Fact, PreserveSyncContext]
        public Task Catch_WithInputValue_CompletedTaskOfCancellation_DoesNotRunContinuationAndDoesNotSwitchContexts()
        {
            // Arrange
            bool ranContinuation = false;
            var syncContext = new Mock<SynchronizationContext> { CallBase = true };
            SynchronizationContext.SetSynchronizationContext(syncContext.Object);

            return TaskHelpers.Canceled<int>()

            // Act
                              .Catch(ex =>
                              {
                                  ranContinuation = true;
                                  return TaskHelpers.FromResult(42);
                              })

            // Assert
                              .ContinueWith(task =>
                              {
                                  Assert.False(ranContinuation);
                                  syncContext.Verify(sc => sc.Post(It.IsAny<SendOrPostCallback>(), null), Times.Never());
                              });
        }

        [Fact, PreserveSyncContext]
        public Task Catch_WithInputValue_CompletedTaskOfFault_RunsOnSameThreadAndDoesNotPostToSynchronizationContext()
        {
            // Arrange
            int outerThreadId = Thread.CurrentThread.ManagedThreadId;
            int innerThreadId = Int32.MinValue;
            Exception thrownException = new Exception();
            Exception caughtException = null;
            var syncContext = new Mock<SynchronizationContext> { CallBase = true };
            SynchronizationContext.SetSynchronizationContext(syncContext.Object);

            return TaskHelpers.FromError<int>(thrownException)

            // Act
                              .Catch(ex =>
                              {
                                  caughtException = ex;
                                  innerThreadId = Thread.CurrentThread.ManagedThreadId;
                                  return TaskHelpers.FromResult(42);
                              })

            // Assert
                              .ContinueWith(task =>
                              {
                                  Assert.Same(thrownException, caughtException);
                                  Assert.Equal(innerThreadId, outerThreadId);
                                  syncContext.Verify(sc => sc.Post(It.IsAny<SendOrPostCallback>(), null), Times.Never());
                              });
        }

        [Fact, PreserveSyncContext]
        public Task Catch_WithInputValue_IncompleteTaskOfSuccess_DoesNotRunContinuationAndDoesNotSwitchContexts()
        {
            // Arrange
            bool ranContinuation = false;
            var syncContext = new Mock<SynchronizationContext> { CallBase = true };
            SynchronizationContext.SetSynchronizationContext(syncContext.Object);

            Task<int> incompleteTask = new Task<int>(() => 42);

            // Act
            Task resultTask = incompleteTask.Catch(ex =>
            {
                ranContinuation = true;
                return TaskHelpers.Completed();
            });

            // Assert
            incompleteTask.Start();

            return resultTask.ContinueWith(task =>
            {
                Assert.False(ranContinuation);
                syncContext.Verify(sc => sc.Post(It.IsAny<SendOrPostCallback>(), null), Times.Never());
            });
        }

        [Fact, PreserveSyncContext]
        public Task Catch_WithInputValue_IncompleteTaskOfCancellation_DoesNotRunContinuationAndDoesNotSwitchContexts()
        {
            // Arrange
            bool ranContinuation = false;
            var syncContext = new Mock<SynchronizationContext> { CallBase = true };
            SynchronizationContext.SetSynchronizationContext(syncContext.Object);

            Task<int> incompleteTask = new Task<int>(() => 42);
            Task resultTask = incompleteTask.ContinueWith(task => TaskHelpers.Canceled<int>()).Unwrap();

            // Act
            resultTask = resultTask.Catch(ex =>
            {
                ranContinuation = true;
                return TaskHelpers.Completed();
            });

            // Assert
            incompleteTask.Start();

            return resultTask.ContinueWith(task =>
            {
                Assert.False(ranContinuation);
                syncContext.Verify(sc => sc.Post(It.IsAny<SendOrPostCallback>(), null), Times.Never());
            });
        }

        [Fact, PreserveSyncContext]
        public Task Catch_WithInputValue_IncompleteTaskOfFault_RunsOnNewThreadAndPostsToSynchronizationContext()
        {
            // Arrange
            int outerThreadId = Thread.CurrentThread.ManagedThreadId;
            int innerThreadId = Int32.MinValue;
            Exception thrownException = new Exception();
            Exception caughtException = null;
            var syncContext = new Mock<SynchronizationContext> { CallBase = true };
            SynchronizationContext.SetSynchronizationContext(syncContext.Object);

            Task<int> incompleteTask = new Task<int>(() => { throw thrownException; });

            // Act
            Task resultTask = incompleteTask.Catch(ex =>
            {
                caughtException = ex;
                innerThreadId = Thread.CurrentThread.ManagedThreadId;
                return TaskHelpers.FromResult(42);
            });

            // Assert
            incompleteTask.Start();

            return resultTask.ContinueWith(task =>
            {
                Assert.Same(thrownException, caughtException);
                Assert.NotEqual(innerThreadId, outerThreadId);
                syncContext.Verify(sc => sc.Post(It.IsAny<SendOrPostCallback>(), null), Times.Once());
            });
        }

        // -----------------------------------------------------------------
        //  Task.CopyResultToCompletionSource(Task)

        [Fact]
        public Task CopyResultToCompletionSource_NoInputValue_SuccessfulTask()
        {
            // Arrange
            var tcs = new TaskCompletionSource<object>();
            var expectedResult = new object();

            return TaskHelpers.Completed()

            // Act
                              .CopyResultToCompletionSource(tcs, expectedResult)

            // Assert
                              .ContinueWith(task =>
                              {
                                  Assert.Equal(TaskStatus.RanToCompletion, task.Status); // Outer task always runs to completion
                                  Assert.Equal(TaskStatus.RanToCompletion, tcs.Task.Status);
                                  Assert.Same(expectedResult, tcs.Task.Result);
                              });
        }

        [Fact]
        public Task CopyResultToCompletionSource_NoInputValue_FaultedTask()
        {
            // Arrange
            var tcs = new TaskCompletionSource<object>();
            var expectedException = new NotImplementedException();

            return TaskHelpers.FromError(expectedException)

            // Act
                              .CopyResultToCompletionSource(tcs, completionResult: null)

            // Assert
                              .ContinueWith(task =>
                              {
                                  Assert.Equal(TaskStatus.RanToCompletion, task.Status); // Outer task always runs to completion
                                  Assert.Equal(TaskStatus.Faulted, tcs.Task.Status);
                                  Assert.Same(expectedException, tcs.Task.Exception.GetBaseException());
                              });
        }

        [Fact]
        public Task CopyResultToCompletionSource_NoInputValue_Canceled()
        {
            // Arrange
            var tcs = new TaskCompletionSource<object>();

            return TaskHelpers.Canceled()

            // Act
                              .CopyResultToCompletionSource(tcs, completionResult: null)

            // Assert
                              .ContinueWith(task =>
                              {
                                  Assert.Equal(TaskStatus.RanToCompletion, task.Status); // Outer task always runs to completion
                                  Assert.Equal(TaskStatus.Canceled, tcs.Task.Status);
                              });
        }

        // -----------------------------------------------------------------
        //  Task.CopyResultToCompletionSource(Task<T>)

        [Fact]
        public Task CopyResultToCompletionSource_WithInputValue_SuccessfulTask()
        {
            // Arrange
            var tcs = new TaskCompletionSource<int>();

            return TaskHelpers.FromResult(42)

            // Act
                              .CopyResultToCompletionSource(tcs)

            // Assert
                              .ContinueWith(task =>
                              {
                                  Assert.Equal(TaskStatus.RanToCompletion, task.Status); // Outer task always runs to completion
                                  Assert.Equal(TaskStatus.RanToCompletion, tcs.Task.Status);
                                  Assert.Equal(42, tcs.Task.Result);
                              });
        }

        [Fact]
        public Task CopyResultToCompletionSource_WithInputValue_FaultedTask()
        {
            // Arrange
            var tcs = new TaskCompletionSource<int>();
            var expectedException = new NotImplementedException();

            return TaskHelpers.FromError<int>(expectedException)

            // Act
                              .CopyResultToCompletionSource(tcs)

            // Assert
                              .ContinueWith(task =>
                              {
                                  Assert.Equal(TaskStatus.RanToCompletion, task.Status); // Outer task always runs to completion
                                  Assert.Equal(TaskStatus.Faulted, tcs.Task.Status);
                                  Assert.Same(expectedException, tcs.Task.Exception.GetBaseException());
                              });
        }

        [Fact]
        public Task CopyResultToCompletionSource_WithInputValue_Canceled()
        {
            // Arrange
            var tcs = new TaskCompletionSource<int>();

            return TaskHelpers.Canceled<int>()

            // Act
                              .CopyResultToCompletionSource(tcs)

            // Assert
                              .ContinueWith(task =>
                              {
                                  Assert.Equal(TaskStatus.RanToCompletion, task.Status); // Outer task always runs to completion
                                  Assert.Equal(TaskStatus.Canceled, tcs.Task.Status);
                              });
        }

        // -----------------------------------------------------------------
        //  Task.Finally(Action)

        [Fact, PreserveSyncContext]
        public Task Finally_NoInputValue_CompletedTaskOfSuccess_RunsOnSameThreadAndDoesNotPostToSynchronizationContext()
        {
            // Arrange
            int originalThreadId = Thread.CurrentThread.ManagedThreadId;
            int callbackThreadId = Int32.MinValue;
            var syncContext = new Mock<SynchronizationContext> { CallBase = true };
            SynchronizationContext.SetSynchronizationContext(syncContext.Object);

            return TaskHelpers.Completed()

            // Act
                              .Finally(() =>
                              {
                                  callbackThreadId = Thread.CurrentThread.ManagedThreadId;
                              })

            // Assert
                              .ContinueWith(task =>
                              {
                                  Assert.Equal(originalThreadId, callbackThreadId);
                                  syncContext.Verify(sc => sc.Post(It.IsAny<SendOrPostCallback>(), null), Times.Never());
                              });
        }

        [Fact, PreserveSyncContext]
        public Task Finally_NoInputValue_CompletedTaskOfCancellation_RunsOnSameThreadAndDoesNotPostToSynchronizationContext()
        {
            // Arrange
            int originalThreadId = Thread.CurrentThread.ManagedThreadId;
            int callbackThreadId = Int32.MinValue;
            var syncContext = new Mock<SynchronizationContext> { CallBase = true };
            SynchronizationContext.SetSynchronizationContext(syncContext.Object);

            return TaskHelpers.Canceled()

            // Act
                              .Finally(() =>
                              {
                                  callbackThreadId = Thread.CurrentThread.ManagedThreadId;
                              })

            // Assert
                              .ContinueWith(task =>
                              {
                                  Assert.Equal(originalThreadId, callbackThreadId);
                                  syncContext.Verify(sc => sc.Post(It.IsAny<SendOrPostCallback>(), null), Times.Never());
                              });
        }

        [Fact, PreserveSyncContext]
        public Task Finally_NoInputValue_CompletedTaskOfFault_RunsOnSameThreadAndDoesNotPostToSynchronizationContext()
        {
            // Arrange
            int originalThreadId = Thread.CurrentThread.ManagedThreadId;
            int callbackThreadId = Int32.MinValue;
            var syncContext = new Mock<SynchronizationContext> { CallBase = true };
            SynchronizationContext.SetSynchronizationContext(syncContext.Object);

            return TaskHelpers.FromError(new InvalidOperationException())

            // Act
                              .Finally(() =>
                              {
                                  callbackThreadId = Thread.CurrentThread.ManagedThreadId;
                              })

            // Assert
                              .ContinueWith(task =>
                              {
                                  var ex = task.Exception;  // Observe the exception
                                  Assert.Equal(originalThreadId, callbackThreadId);
                                  syncContext.Verify(sc => sc.Post(It.IsAny<SendOrPostCallback>(), null), Times.Never());
                              });
        }

        [Fact, PreserveSyncContext]
        public Task Finally_NoInputValue_IncompleteTaskOfSuccess_RunsOnNewThreadAndPostsContinuationToSynchronizationContext()
        {
            // Arrange
            int originalThreadId = Thread.CurrentThread.ManagedThreadId;
            int callbackThreadId = Int32.MinValue;
            var syncContext = new Mock<SynchronizationContext> { CallBase = true };
            SynchronizationContext.SetSynchronizationContext(syncContext.Object);

            Task incompleteTask = new Task(() => { });

            // Act
            Task resultTask = incompleteTask.Finally(() =>
            {
                callbackThreadId = Thread.CurrentThread.ManagedThreadId;
            });

            // Assert
            incompleteTask.Start();

            return resultTask.ContinueWith(task =>
            {
                Assert.NotEqual(originalThreadId, callbackThreadId);
                syncContext.Verify(sc => sc.Post(It.IsAny<SendOrPostCallback>(), null), Times.Once());
            });
        }

        [Fact, PreserveSyncContext]
        public Task Finally_NoInputValue_IncompleteTaskOfCancellation_RunsOnNewThreadAndPostsContinuationToSynchronizationContext()
        {
            // Arrange
            int originalThreadId = Thread.CurrentThread.ManagedThreadId;
            int callbackThreadId = Int32.MinValue;
            var syncContext = new Mock<SynchronizationContext> { CallBase = true };
            SynchronizationContext.SetSynchronizationContext(syncContext.Object);

            Task incompleteTask = new Task(() => { });
            Task resultTask = incompleteTask.ContinueWith(task => TaskHelpers.Canceled()).Unwrap();

            // Act
            resultTask = resultTask.Finally(() =>
            {
                callbackThreadId = Thread.CurrentThread.ManagedThreadId;
            });

            // Assert
            incompleteTask.Start();

            return resultTask.ContinueWith(task =>
            {
                Assert.NotEqual(originalThreadId, callbackThreadId);
                syncContext.Verify(sc => sc.Post(It.IsAny<SendOrPostCallback>(), null), Times.Once());
            });
        }

        [Fact, PreserveSyncContext]
        public Task Finally_NoInputValue_IncompleteTaskOfFault_RunsOnNewThreadAndPostsContinuationToSynchronizationContext()
        {
            // Arrange
            int originalThreadId = Thread.CurrentThread.ManagedThreadId;
            int callbackThreadId = Int32.MinValue;
            var syncContext = new Mock<SynchronizationContext> { CallBase = true };
            SynchronizationContext.SetSynchronizationContext(syncContext.Object);

            Task incompleteTask = new Task(() => { throw new InvalidOperationException(); });

            // Act
            Task resultTask = incompleteTask.Finally(() =>
            {
                callbackThreadId = Thread.CurrentThread.ManagedThreadId;
            });

            // Assert
            incompleteTask.Start();

            return resultTask.ContinueWith(task =>
            {
                var ex = task.Exception;  // Observe the exception
                Assert.NotEqual(originalThreadId, callbackThreadId);
                syncContext.Verify(sc => sc.Post(It.IsAny<SendOrPostCallback>(), null), Times.Once());
            });
        }

        // -----------------------------------------------------------------
        //  Task<T>.Finally(Action)

        [Fact, PreserveSyncContext]
        public Task Finally_WithInputValue_CompletedTaskOfSuccess_RunsOnSameThreadAndDoesNotPostToSynchronizationContext()
        {
            // Arrange
            int originalThreadId = Thread.CurrentThread.ManagedThreadId;
            int callbackThreadId = Int32.MinValue;
            var syncContext = new Mock<SynchronizationContext> { CallBase = true };
            SynchronizationContext.SetSynchronizationContext(syncContext.Object);

            return TaskHelpers.FromResult(21)

            // Act
                              .Finally(() =>
                              {
                                  callbackThreadId = Thread.CurrentThread.ManagedThreadId;
                              })

            // Assert
                              .ContinueWith(task =>
                              {
                                  Assert.Equal(21, task.Result);
                                  Assert.Equal(originalThreadId, callbackThreadId);
                                  syncContext.Verify(sc => sc.Post(It.IsAny<SendOrPostCallback>(), null), Times.Never());
                              });
        }

        [Fact, PreserveSyncContext]
        public Task Finally_WithInputValue_CompletedTaskOfCancellation_RunsOnSameThreadAndDoesNotPostToSynchronizationContext()
        {
            // Arrange
            int originalThreadId = Thread.CurrentThread.ManagedThreadId;
            int callbackThreadId = Int32.MinValue;
            var syncContext = new Mock<SynchronizationContext> { CallBase = true };
            SynchronizationContext.SetSynchronizationContext(syncContext.Object);

            return TaskHelpers.Canceled<int>()

            // Act
                              .Finally(() =>
                              {
                                  callbackThreadId = Thread.CurrentThread.ManagedThreadId;
                              })

            // Assert
                              .ContinueWith(task =>
                              {
                                  Assert.Equal(originalThreadId, callbackThreadId);
                                  syncContext.Verify(sc => sc.Post(It.IsAny<SendOrPostCallback>(), null), Times.Never());
                              });
        }

        [Fact, PreserveSyncContext]
        public Task Finally_WithInputValue_CompletedTaskOfFault_RunsOnSameThreadAndDoesNotPostToSynchronizationContext()
        {
            // Arrange
            int originalThreadId = Thread.CurrentThread.ManagedThreadId;
            int callbackThreadId = Int32.MinValue;
            var syncContext = new Mock<SynchronizationContext> { CallBase = true };
            SynchronizationContext.SetSynchronizationContext(syncContext.Object);

            return TaskHelpers.FromError<int>(new InvalidOperationException())

            // Act
                              .Finally(() =>
                              {
                                  callbackThreadId = Thread.CurrentThread.ManagedThreadId;
                              })

            // Assert
                              .ContinueWith(task =>
                              {
                                  var ex = task.Exception;  // Observe the exception
                                  Assert.Equal(originalThreadId, callbackThreadId);
                                  syncContext.Verify(sc => sc.Post(It.IsAny<SendOrPostCallback>(), null), Times.Never());
                              });
        }

        [Fact, PreserveSyncContext]
        public Task Finally_WithInputValue_IncompleteTaskOfSuccess_RunsOnNewThreadAndPostsContinuationToSynchronizationContext()
        {
            // Arrange
            int originalThreadId = Thread.CurrentThread.ManagedThreadId;
            int callbackThreadId = Int32.MinValue;
            var syncContext = new Mock<SynchronizationContext> { CallBase = true };
            SynchronizationContext.SetSynchronizationContext(syncContext.Object);

            Task incompleteTask = new Task<int>(() => 21);

            // Act
            Task resultTask = incompleteTask.Finally(() =>
            {
                callbackThreadId = Thread.CurrentThread.ManagedThreadId;
            });

            // Assert
            incompleteTask.Start();

            return resultTask.ContinueWith(task =>
            {
                Assert.NotEqual(originalThreadId, callbackThreadId);
                syncContext.Verify(sc => sc.Post(It.IsAny<SendOrPostCallback>(), null), Times.Once());
            });
        }

        [Fact, PreserveSyncContext]
        public Task Finally_WithInputValue_IncompleteTaskOfCancellation_RunsOnNewThreadAndPostsContinuationToSynchronizationContext()
        {
            // Arrange
            int originalThreadId = Thread.CurrentThread.ManagedThreadId;
            int callbackThreadId = Int32.MinValue;
            var syncContext = new Mock<SynchronizationContext> { CallBase = true };
            SynchronizationContext.SetSynchronizationContext(syncContext.Object);

            Task<int> incompleteTask = new Task<int>(() => 42);
            Task resultTask = incompleteTask.ContinueWith(task => TaskHelpers.Canceled<int>()).Unwrap();

            // Act
            resultTask = resultTask.Finally(() =>
            {
                callbackThreadId = Thread.CurrentThread.ManagedThreadId;
            });

            // Assert
            incompleteTask.Start();

            return resultTask.ContinueWith(task =>
            {
                Assert.NotEqual(originalThreadId, callbackThreadId);
                syncContext.Verify(sc => sc.Post(It.IsAny<SendOrPostCallback>(), null), Times.Once());
            });
        }

        [Fact, PreserveSyncContext]
        public Task Finally_WithInputValue_IncompleteTaskOfFault_RunsOnNewThreadAndPostsContinuationToSynchronizationContext()
        {
            // Arrange
            int originalThreadId = Thread.CurrentThread.ManagedThreadId;
            int callbackThreadId = Int32.MinValue;
            var syncContext = new Mock<SynchronizationContext> { CallBase = true };
            SynchronizationContext.SetSynchronizationContext(syncContext.Object);

            Task<int> incompleteTask = new Task<int>(() => { throw new InvalidOperationException(); });

            // Act
            Task resultTask = incompleteTask.Finally(() =>
            {
                callbackThreadId = Thread.CurrentThread.ManagedThreadId;
            });

            // Assert
            incompleteTask.Start();

            return resultTask.ContinueWith(task =>
            {
                var ex = task.Exception;  // Observe the exception
                Assert.NotEqual(originalThreadId, callbackThreadId);
                syncContext.Verify(sc => sc.Post(It.IsAny<SendOrPostCallback>(), null), Times.Once());
            });
        }

        // -----------------------------------------------------------------
        //  Task Task.Then(Action)

        [Fact]
        public Task Then_NoInputValue_NoReturnValue_CallsContinuation()
        {
            // Arrange
            bool ranContinuation = false;

            return TaskHelpers.Completed()

            // Act
                              .Then(() =>
                              {
                                  ranContinuation = true;
                              })

            // Assert
                              .ContinueWith(task =>
                              {
                                  Assert.Equal(TaskStatus.RanToCompletion, task.Status);
                                  Assert.True(ranContinuation);
                              });
        }

        [Fact]
        public Task Then_NoInputValue_NoReturnValue_ThrownExceptionIsPropagated()
        {
            // Arrange
            return TaskHelpers.Completed()

            // Act
                              .Then(() =>
                              {
                                  throw new NotImplementedException();
                              })

            // Assert
                              .ContinueWith(task =>
                              {
                                  Assert.Equal(TaskStatus.Faulted, task.Status);
                                  var ex = Assert.Single(task.Exception.InnerExceptions);
                                  Assert.IsType<NotImplementedException>(ex);
                              });
        }

        [Fact]
        public Task Then_NoInputValue_NoReturnValue_FaultPreventsFurtherThenStatementsFromExecuting()
        {
            // Arrange
            bool ranContinuation = false;

            return TaskHelpers.FromError(new NotImplementedException())

            // Act
                              .Then(() =>
                              {
                                  ranContinuation = true;
                              })

            // Assert
                              .ContinueWith(task =>
                              {
                                  var ex = task.Exception;  // Observe the exception
                                  Assert.False(ranContinuation);
                              });
        }

        [Fact]
        public Task Then_NoInputValue_NoReturnValue_ManualCancellationPreventsFurtherThenStatementsFromExecuting()
        {
            // Arrange
            bool ranContinuation = false;

            return TaskHelpers.Canceled()

            // Act
                              .Then(() =>
                              {
                                  ranContinuation = true;
                              })

            // Assert
                              .ContinueWith(task =>
                              {
                                  Assert.Equal(TaskStatus.Canceled, task.Status);
                                  Assert.False(ranContinuation);
                              });
        }

        [Fact]
        public Task Then_NoInputValue_NoReturnValue_TokenCancellationPreventsFurtherThenStatementsFromExecuting()
        {
            // Arrange
            bool ranContinuation = false;
            CancellationToken cancellationToken = new CancellationToken(canceled: true);

            return TaskHelpers.Completed()

            // Act
                              .Then(() =>
                              {
                                  ranContinuation = true;
                              }, cancellationToken)

            // Assert
                              .ContinueWith(task =>
                              {
                                  Assert.Equal(TaskStatus.Canceled, task.Status);
                                  Assert.False(ranContinuation);
                              });
        }

        [Fact, PreserveSyncContext]
        public Task Then_NoInputValue_NoReturnValue_IncompleteTask_RunsOnNewThreadAndPostsContinuationToSynchronizationContext()
        {
            // Arrange
            int originalThreadId = Thread.CurrentThread.ManagedThreadId;
            int callbackThreadId = Int32.MinValue;
            var syncContext = new Mock<SynchronizationContext> { CallBase = true };
            SynchronizationContext.SetSynchronizationContext(syncContext.Object);

            Task incompleteTask = new Task(() => { });

            // Act
            Task resultTask = incompleteTask.Then(() =>
            {
                callbackThreadId = Thread.CurrentThread.ManagedThreadId;
            });

            // Assert
            incompleteTask.Start();

            return resultTask.ContinueWith(task =>
            {
                Assert.NotEqual(originalThreadId, callbackThreadId);
                syncContext.Verify(sc => sc.Post(It.IsAny<SendOrPostCallback>(), null), Times.Once());
            });
        }

        [Fact, PreserveSyncContext]
        public Task Then_NoInputValue_NoReturnValue_CompleteTask_RunsOnSameThreadAndDoesNotPostToSynchronizationContext()
        {
            // Arrange
            int originalThreadId = Thread.CurrentThread.ManagedThreadId;
            int callbackThreadId = Int32.MinValue;
            var syncContext = new Mock<SynchronizationContext> { CallBase = true };
            SynchronizationContext.SetSynchronizationContext(syncContext.Object);

            return TaskHelpers.Completed()

            // Act
                              .Then(() =>
                              {
                                  callbackThreadId = Thread.CurrentThread.ManagedThreadId;
                              })

            // Assert
                              .ContinueWith(task =>
                              {
                                  Assert.Equal(originalThreadId, callbackThreadId);
                                  syncContext.Verify(sc => sc.Post(It.IsAny<SendOrPostCallback>(), null), Times.Never());
                              });
        }

        // -----------------------------------------------------------------
        //  Task Task.Then(Func<Task>)

        [Fact]
        public Task Then_NoInputValue_ReturnsTask_CallsContinuation()
        {
            // Arrange
            bool ranContinuation = false;

            return TaskHelpers.Completed()

            // Act
                              .Then(() =>
                              {
                                  ranContinuation = true;
                                  return TaskHelpers.Completed();
                              })

            // Assert
                              .ContinueWith(task =>
                              {
                                  Assert.Equal(TaskStatus.RanToCompletion, task.Status);
                                  Assert.True(ranContinuation);
                              });
        }

        [Fact]
        public Task Then_NoInputValue_ReturnsTask_ThrownExceptionIsPropagated()
        {
            // Arrange
            return TaskHelpers.Completed()

            // Act
                              .Then(() =>
                              {
                                  throw new NotImplementedException();
                                  return TaskHelpers.Completed();  // Return-after-throw to guarantee correct lambda signature
                              })

            // Assert
                              .ContinueWith(task =>
                              {
                                  Assert.Equal(TaskStatus.Faulted, task.Status);
                                  var ex = Assert.Single(task.Exception.InnerExceptions);
                                  Assert.IsType<NotImplementedException>(ex);
                              });
        }

        [Fact]
        public Task Then_NoInputValue_ReturnsTask_FaultPreventsFurtherThenStatementsFromExecuting()
        {
            // Arrange
            bool ranContinuation = false;

            return TaskHelpers.FromError(new NotImplementedException())

            // Act
                              .Then(() =>
                              {
                                  ranContinuation = true;
                                  return TaskHelpers.Completed();
                              })

            // Assert
                              .ContinueWith(task =>
                              {
                                  var ex = task.Exception;  // Observe the exception
                                  Assert.False(ranContinuation);
                              });
        }

        [Fact]
        public Task Then_NoInputValue_ReturnsTask_ManualCancellationPreventsFurtherThenStatementsFromExecuting()
        {
            // Arrange
            bool ranContinuation = false;

            return TaskHelpers.Canceled()

            // Act
                              .Then(() =>
                              {
                                  ranContinuation = true;
                                  return TaskHelpers.Completed();
                              })

            // Assert
                              .ContinueWith(task =>
                              {
                                  Assert.Equal(TaskStatus.Canceled, task.Status);
                                  Assert.False(ranContinuation);
                              });
        }

        [Fact]
        public Task Then_NoInputValue_ReturnsTask_TokenCancellationPreventsFurtherThenStatementsFromExecuting()
        {
            // Arrange
            bool ranContinuation = false;
            CancellationToken cancellationToken = new CancellationToken(canceled: true);

            return TaskHelpers.Completed()

            // Act
                              .Then(() =>
                              {
                                  ranContinuation = true;
                                  return TaskHelpers.Completed();
                              }, cancellationToken)

            // Assert
                              .ContinueWith(task =>
                              {
                                  Assert.Equal(TaskStatus.Canceled, task.Status);
                                  Assert.False(ranContinuation);
                              });
        }

        [Fact, PreserveSyncContext]
        public Task Then_NoInputValue_ReturnsTask_IncompleteTask_RunsOnNewThreadAndPostsContinuationToSynchronizationContext()
        {
            // Arrange
            int originalThreadId = Thread.CurrentThread.ManagedThreadId;
            int callbackThreadId = Int32.MinValue;
            var syncContext = new Mock<SynchronizationContext> { CallBase = true };
            SynchronizationContext.SetSynchronizationContext(syncContext.Object);

            Task incompleteTask = new Task(() => { });

            // Act
            Task resultTask = incompleteTask.Then(() =>
            {
                callbackThreadId = Thread.CurrentThread.ManagedThreadId;
                return TaskHelpers.Completed();
            });

            // Assert
            incompleteTask.Start();

            return resultTask.ContinueWith(task =>
            {
                Assert.NotEqual(originalThreadId, callbackThreadId);
                syncContext.Verify(sc => sc.Post(It.IsAny<SendOrPostCallback>(), null), Times.Once());
            });
        }

        [Fact, PreserveSyncContext]
        public Task Then_NoInputValue_ReturnsTask_CompleteTask_RunsOnSameThreadAndDoesNotPostToSynchronizationContext()
        {
            // Arrange
            int originalThreadId = Thread.CurrentThread.ManagedThreadId;
            int callbackThreadId = Int32.MinValue;
            var syncContext = new Mock<SynchronizationContext> { CallBase = true };
            SynchronizationContext.SetSynchronizationContext(syncContext.Object);

            return TaskHelpers.Completed()

            // Act
                              .Then(() =>
                              {
                                  callbackThreadId = Thread.CurrentThread.ManagedThreadId;
                                  return TaskHelpers.Completed();
                              })

            // Assert
                              .ContinueWith(task =>
                              {
                                  Assert.Equal(originalThreadId, callbackThreadId);
                                  syncContext.Verify(sc => sc.Post(It.IsAny<SendOrPostCallback>(), null), Times.Never());
                              });
        }

        // -----------------------------------------------------------------
        //  Task<T> Task.Then(Func<T>)

        [Fact]
        public Task Then_NoInputValue_WithReturnValue_CallsContinuation()
        {
            // Arrange
            return TaskHelpers.Completed()

            // Act
                              .Then(() =>
                              {
                                  return 42;
                              })

            // Assert
                              .ContinueWith(task =>
                              {
                                  Assert.Equal(TaskStatus.RanToCompletion, task.Status);
                                  Assert.Equal(42, task.Result);
                              });
        }

        [Fact]
        public Task Then_NoInputValue_WithReturnValue_ThrownExceptionIsPropagated()
        {
            // Arrange
            return TaskHelpers.Completed()

            // Act
                              .Then(() =>
                              {
                                  throw new NotImplementedException();
                                  return 0;  // Return-after-throw to guarantee correct lambda signature
                              })

            // Assert
                              .ContinueWith(task =>
                              {
                                  Assert.Equal(TaskStatus.Faulted, task.Status);
                                  var ex = Assert.Single(task.Exception.InnerExceptions);
                                  Assert.IsType<NotImplementedException>(ex);
                              });
        }

        [Fact]
        public Task Then_NoInputValue_WithReturnValue_FaultPreventsFurtherThenStatementsFromExecuting()
        {
            // Arrange
            bool ranContinuation = false;

            return TaskHelpers.FromError(new NotImplementedException())

            // Act
                              .Then(() =>
                              {
                                  ranContinuation = true;
                                  return 42;
                              })

            // Assert
                              .ContinueWith(task =>
                              {
                                  var ex = task.Exception;  // Observe the exception
                                  Assert.False(ranContinuation);
                              });
        }

        [Fact]
        public Task Then_NoInputValue_WithReturnValue_ManualCancellationPreventsFurtherThenStatementsFromExecuting()
        {
            // Arrange
            bool ranContinuation = false;

            return TaskHelpers.Canceled()

            // Act
                              .Then(() =>
                              {
                                  ranContinuation = true;
                                  return 42;
                              })

            // Assert
                              .ContinueWith(task =>
                              {
                                  Assert.Equal(TaskStatus.Canceled, task.Status);
                                  Assert.False(ranContinuation);
                              });
        }

        [Fact]
        public Task Then_NoInputValue_WithReturnValue_TokenCancellationPreventsFurtherThenStatementsFromExecuting()
        {
            // Arrange
            bool ranContinuation = false;
            CancellationToken cancellationToken = new CancellationToken(canceled: true);

            return TaskHelpers.Completed()

            // Act
                              .Then(() =>
                              {
                                  ranContinuation = true;
                                  return 42;
                              }, cancellationToken)

            // Assert
                              .ContinueWith(task =>
                              {
                                  Assert.Equal(TaskStatus.Canceled, task.Status);
                                  Assert.False(ranContinuation);
                              });
        }

        [Fact, PreserveSyncContext]
        public Task Then_NoInputValue_WithReturnValue_IncompleteTask_RunsOnNewThreadAndPostsContinuationToSynchronizationContext()
        {
            // Arrange
            int originalThreadId = Thread.CurrentThread.ManagedThreadId;
            int callbackThreadId = Int32.MinValue;
            var syncContext = new Mock<SynchronizationContext> { CallBase = true };
            SynchronizationContext.SetSynchronizationContext(syncContext.Object);

            Task incompleteTask = new Task(() => { });

            // Act
            Task resultTask = incompleteTask.Then(() =>
            {
                callbackThreadId = Thread.CurrentThread.ManagedThreadId;
                return 42;
            });

            // Assert
            incompleteTask.Start();

            return resultTask.ContinueWith(task =>
            {
                Assert.NotEqual(originalThreadId, callbackThreadId);
                syncContext.Verify(sc => sc.Post(It.IsAny<SendOrPostCallback>(), null), Times.Once());
            });
        }

        [Fact, PreserveSyncContext]
        public Task Then_NoInputValue_WithReturnValue_CompleteTask_RunsOnSameThreadAndDoesNotPostToSynchronizationContext()
        {
            // Arrange
            int originalThreadId = Thread.CurrentThread.ManagedThreadId;
            int callbackThreadId = Int32.MinValue;
            var syncContext = new Mock<SynchronizationContext> { CallBase = true };
            SynchronizationContext.SetSynchronizationContext(syncContext.Object);

            return TaskHelpers.Completed()

            // Act
                              .Then(() =>
                              {
                                  callbackThreadId = Thread.CurrentThread.ManagedThreadId;
                                  return 42;
                              })

            // Assert
                              .ContinueWith(task =>
                              {
                                  Assert.Equal(originalThreadId, callbackThreadId);
                                  syncContext.Verify(sc => sc.Post(It.IsAny<SendOrPostCallback>(), null), Times.Never());
                              });
        }

        // -----------------------------------------------------------------
        //  Task<T> Task.Then(Func<Task<T>>)

        [Fact]
        public Task Then_NoInputValue_WithTaskReturnValue_CallsContinuation()
        {
            // Arrange
            return TaskHelpers.Completed()

            // Act
                              .Then(() =>
                              {
                                  return TaskHelpers.FromResult(42);
                              })

            // Assert
                              .ContinueWith(task =>
                              {
                                  Assert.Equal(TaskStatus.RanToCompletion, task.Status);
                                  Assert.Equal(42, task.Result);
                              });
        }

        [Fact]
        public Task Then_NoInputValue_WithTaskReturnValue_ThrownExceptionIsPropagated()
        {
            // Arrange
            return TaskHelpers.Completed()

            // Act
                              .Then(() =>
                              {
                                  throw new NotImplementedException();
                                  return TaskHelpers.FromResult(0);  // Return-after-throw to guarantee correct lambda signature
                              })

            // Assert
                              .ContinueWith(task =>
                              {
                                  Assert.Equal(TaskStatus.Faulted, task.Status);
                                  var ex = Assert.Single(task.Exception.InnerExceptions);
                                  Assert.IsType<NotImplementedException>(ex);
                              });
        }

        [Fact]
        public Task Then_NoInputValue_WithTaskReturnValue_FaultPreventsFurtherThenStatementsFromExecuting()
        {
            // Arrange
            bool ranContinuation = false;

            return TaskHelpers.FromError(new NotImplementedException())

            // Act
                              .Then(() =>
                              {
                                  ranContinuation = true;
                                  return TaskHelpers.FromResult(42);
                              })

            // Assert
                              .ContinueWith(task =>
                              {
                                  var ex = task.Exception;  // Observe the exception
                                  Assert.False(ranContinuation);
                              });
        }

        [Fact]
        public Task Then_NoInputValue_WithTaskReturnValue_ManualCancellationPreventsFurtherThenStatementsFromExecuting()
        {
            // Arrange
            bool ranContinuation = false;

            return TaskHelpers.Canceled()

            // Act
                              .Then(() =>
                              {
                                  ranContinuation = true;
                                  return TaskHelpers.FromResult(42);
                              })

            // Assert
                              .ContinueWith(task =>
                              {
                                  Assert.Equal(TaskStatus.Canceled, task.Status);
                                  Assert.False(ranContinuation);
                              });
        }

        [Fact]
        public Task Then_NoInputValue_WithTaskReturnValue_TokenCancellationPreventsFurtherThenStatementsFromExecuting()
        {
            // Arrange
            bool ranContinuation = false;
            CancellationToken cancellationToken = new CancellationToken(canceled: true);

            return TaskHelpers.Completed()

            // Act
                              .Then(() =>
                              {
                                  ranContinuation = true;
                                  return TaskHelpers.FromResult(42);
                              }, cancellationToken)

            // Assert
                              .ContinueWith(task =>
                              {
                                  Assert.Equal(TaskStatus.Canceled, task.Status);
                                  Assert.False(ranContinuation);
                              });
        }

        [Fact, PreserveSyncContext]
        public Task Then_NoInputValue_WithTaskReturnValue_IncompleteTask_RunsOnNewThreadAndPostsContinuationToSynchronizationContext()
        {
            // Arrange
            int originalThreadId = Thread.CurrentThread.ManagedThreadId;
            int callbackThreadId = Int32.MinValue;
            var syncContext = new Mock<SynchronizationContext> { CallBase = true };
            SynchronizationContext.SetSynchronizationContext(syncContext.Object);

            Task incompleteTask = new Task(() => { });

            // Act
            Task resultTask = incompleteTask.Then(() =>
            {
                callbackThreadId = Thread.CurrentThread.ManagedThreadId;
                return TaskHelpers.FromResult(42);
            });

            // Assert
            incompleteTask.Start();

            return resultTask.ContinueWith(task =>
            {
                Assert.NotEqual(originalThreadId, callbackThreadId);
                syncContext.Verify(sc => sc.Post(It.IsAny<SendOrPostCallback>(), null), Times.Once());
            });
        }

        [Fact, PreserveSyncContext]
        public Task Then_NoInputValue_WithTaskReturnValue_CompleteTask_RunsOnSameThreadAndDoesNotPostToSynchronizationContext()
        {
            // Arrange
            int originalThreadId = Thread.CurrentThread.ManagedThreadId;
            int callbackThreadId = Int32.MinValue;
            var syncContext = new Mock<SynchronizationContext> { CallBase = true };
            SynchronizationContext.SetSynchronizationContext(syncContext.Object);

            return TaskHelpers.Completed()

            // Act
                              .Then(() =>
                              {
                                  callbackThreadId = Thread.CurrentThread.ManagedThreadId;
                                  return TaskHelpers.FromResult(42);
                              })

            // Assert
                              .ContinueWith(task =>
                              {
                                  Assert.Equal(originalThreadId, callbackThreadId);
                                  syncContext.Verify(sc => sc.Post(It.IsAny<SendOrPostCallback>(), null), Times.Never());
                              });
        }

        // -----------------------------------------------------------------
        //  Task Task<T>.Then(Action)

        [Fact]
        public Task Then_WithInputValue_NoReturnValue_CallsContinuationWithPriorTaskResult()
        {
            // Arrange
            int passedResult = 0;

            return TaskHelpers.FromResult(21)

            // Act
                              .Then(result =>
                              {
                                  passedResult = result;
                              })

            // Assert
                              .ContinueWith(task =>
                              {
                                  Assert.Equal(TaskStatus.RanToCompletion, task.Status);
                                  Assert.Equal(21, passedResult);
                              });
        }

        [Fact]
        public Task Then_WithInputValue_NoReturnValue_ThrownExceptionIsPropagated()
        {
            // Arrange
            return TaskHelpers.FromResult(21)

            // Act
                              .Then(result =>
                              {
                                  throw new NotImplementedException();
                              })

            // Assert
                              .ContinueWith(task =>
                              {
                                  Assert.Equal(TaskStatus.Faulted, task.Status);
                                  var ex = Assert.Single(task.Exception.InnerExceptions);
                                  Assert.IsType<NotImplementedException>(ex);
                              });
        }

        [Fact]
        public Task Then_WithInputValue_NoReturnValue_FaultPreventsFurtherThenStatementsFromExecuting()
        {
            // Arrange
            bool ranContinuation = false;

            return TaskHelpers.FromError<int>(new NotImplementedException())

            // Act
                              .Then(result =>
                              {
                                  ranContinuation = true;
                              })

            // Assert
                              .ContinueWith(task =>
                              {
                                  var ex = task.Exception;  // Observe the exception
                                  Assert.False(ranContinuation);
                              });
        }

        [Fact]
        public Task Then_WithInputValue_NoReturnValue_ManualCancellationPreventsFurtherThenStatementsFromExecuting()
        {
            // Arrange
            bool ranContinuation = false;

            return TaskHelpers.Canceled<int>()

            // Act
                              .Then(result =>
                              {
                                  ranContinuation = true;
                              })

            // Assert
                              .ContinueWith(task =>
                              {
                                  Assert.Equal(TaskStatus.Canceled, task.Status);
                                  Assert.False(ranContinuation);
                              });
        }

        [Fact]
        public Task Then_WithInputValue_NoReturnValue_TokenCancellationPreventsFurtherThenStatementsFromExecuting()
        {
            // Arrange
            bool ranContinuation = false;
            CancellationToken cancellationToken = new CancellationToken(canceled: true);

            return TaskHelpers.FromResult(21)

            // Act
                              .Then(result =>
                              {
                                  ranContinuation = true;
                              }, cancellationToken)

            // Assert
                              .ContinueWith(task =>
                              {
                                  Assert.Equal(TaskStatus.Canceled, task.Status);
                                  Assert.False(ranContinuation);
                              });
        }

        [Fact, PreserveSyncContext]
        public Task Then_WithInputValue_NoReturnValue_IncompleteTask_RunsOnNewThreadAndPostsContinuationToSynchronizationContext()
        {
            // Arrange
            int originalThreadId = Thread.CurrentThread.ManagedThreadId;
            int callbackThreadId = Int32.MinValue;
            var syncContext = new Mock<SynchronizationContext> { CallBase = true };
            SynchronizationContext.SetSynchronizationContext(syncContext.Object);

            Task<int> incompleteTask = new Task<int>(() => 21);

            // Act
            Task resultTask = incompleteTask.Then(result =>
            {
                callbackThreadId = Thread.CurrentThread.ManagedThreadId;
            });

            // Assert
            incompleteTask.Start();

            return resultTask.ContinueWith(task =>
            {
                Assert.NotEqual(originalThreadId, callbackThreadId);
                syncContext.Verify(sc => sc.Post(It.IsAny<SendOrPostCallback>(), null), Times.Once());
            });
        }

        [Fact, PreserveSyncContext]
        public Task Then_WithInputValue_NoReturnValue_CompleteTask_RunsOnSameThreadAndDoesNotPostToSynchronizationContext()
        {
            // Arrange
            int originalThreadId = Thread.CurrentThread.ManagedThreadId;
            int callbackThreadId = Int32.MinValue;
            var syncContext = new Mock<SynchronizationContext> { CallBase = true };
            SynchronizationContext.SetSynchronizationContext(syncContext.Object);

            return TaskHelpers.FromResult(21)

            // Act
                              .Then(result =>
                              {
                                  callbackThreadId = Thread.CurrentThread.ManagedThreadId;
                              })

            // Assert
                              .ContinueWith(task =>
                              {
                                  Assert.Equal(originalThreadId, callbackThreadId);
                                  syncContext.Verify(sc => sc.Post(It.IsAny<SendOrPostCallback>(), null), Times.Never());
                              });
        }

        // -----------------------------------------------------------------
        //  Task<T> Task.Then(Func<T>)

        [Fact]
        public Task Then_WithInputValue_WithReturnValue_CallsContinuation()
        {
            // Arrange
            return TaskHelpers.FromResult(21)

            // Act
                              .Then(result =>
                              {
                                  return 42;
                              })

            // Assert
                              .ContinueWith(task =>
                              {
                                  Assert.Equal(TaskStatus.RanToCompletion, task.Status);
                                  Assert.Equal(42, task.Result);
                              });
        }

        [Fact]
        public Task Then_WithInputValue_WithReturnValue_ThrownExceptionIsPropagated()
        {
            // Arrange
            return TaskHelpers.FromResult(21)

            // Act
                              .Then(result =>
                              {
                                  throw new NotImplementedException();
                                  return 0;  // Return-after-throw to guarantee correct lambda signature
                              })

            // Assert
                              .ContinueWith(task =>
                              {
                                  Assert.Equal(TaskStatus.Faulted, task.Status);
                                  var ex = Assert.Single(task.Exception.InnerExceptions);
                                  Assert.IsType<NotImplementedException>(ex);
                              });
        }

        [Fact]
        public Task Then_WithInputValue_WithReturnValue_FaultPreventsFurtherThenStatementsFromExecuting()
        {
            // Arrange
            bool ranContinuation = false;

            return TaskHelpers.FromError<int>(new NotImplementedException())

            // Act
                              .Then(result =>
                              {
                                  ranContinuation = true;
                                  return 42;
                              })

            // Assert
                              .ContinueWith(task =>
                              {
                                  var ex = task.Exception;  // Observe the exception
                                  Assert.False(ranContinuation);
                              });
        }

        [Fact]
        public Task Then_WithInputValue_WithReturnValue_ManualCancellationPreventsFurtherThenStatementsFromExecuting()
        {
            // Arrange
            bool ranContinuation = false;

            return TaskHelpers.Canceled<int>()

            // Act
                              .Then(result =>
                              {
                                  ranContinuation = true;
                                  return 42;
                              })

            // Assert
                              .ContinueWith(task =>
                              {
                                  Assert.Equal(TaskStatus.Canceled, task.Status);
                                  Assert.False(ranContinuation);
                              });
        }

        [Fact]
        public Task Then_WithInputValue_WithReturnValue_TokenCancellationPreventsFurtherThenStatementsFromExecuting()
        {
            // Arrange
            bool ranContinuation = false;
            CancellationToken cancellationToken = new CancellationToken(canceled: true);

            return TaskHelpers.FromResult(21)

            // Act
                              .Then(result =>
                              {
                                  ranContinuation = true;
                                  return 42;
                              }, cancellationToken)

            // Assert
                              .ContinueWith(task =>
                              {
                                  Assert.Equal(TaskStatus.Canceled, task.Status);
                                  Assert.False(ranContinuation);
                              });
        }

        [Fact, PreserveSyncContext]
        public Task Then_WithInputValue_WithReturnValue_IncompleteTask_RunsOnNewThreadAndPostsContinuationToSynchronizationContext()
        {
            // Arrange
            int originalThreadId = Thread.CurrentThread.ManagedThreadId;
            int callbackThreadId = Int32.MinValue;
            var syncContext = new Mock<SynchronizationContext> { CallBase = true };
            SynchronizationContext.SetSynchronizationContext(syncContext.Object);

            Task<int> incompleteTask = new Task<int>(() => 21);

            // Act
            Task resultTask = incompleteTask.Then(result =>
            {
                callbackThreadId = Thread.CurrentThread.ManagedThreadId;
                return 42;
            });

            // Assert
            incompleteTask.Start();

            return resultTask.ContinueWith(task =>
            {
                Assert.NotEqual(originalThreadId, callbackThreadId);
                syncContext.Verify(sc => sc.Post(It.IsAny<SendOrPostCallback>(), null), Times.Once());
            });
        }

        [Fact, PreserveSyncContext]
        public Task Then_WithInputValue_WithReturnValue_CompleteTask_RunsOnSameThreadAndDoesNotPostToSynchronizationContext()
        {
            // Arrange
            int originalThreadId = Thread.CurrentThread.ManagedThreadId;
            int callbackThreadId = Int32.MinValue;
            var syncContext = new Mock<SynchronizationContext> { CallBase = true };
            SynchronizationContext.SetSynchronizationContext(syncContext.Object);

            return TaskHelpers.FromResult(21)

            // Act
                              .Then(result =>
                              {
                                  callbackThreadId = Thread.CurrentThread.ManagedThreadId;
                                  return 42;
                              })

            // Assert
                              .ContinueWith(task =>
                              {
                                  Assert.Equal(originalThreadId, callbackThreadId);
                                  syncContext.Verify(sc => sc.Post(It.IsAny<SendOrPostCallback>(), null), Times.Never());
                              });
        }

        // -----------------------------------------------------------------
        //  Task<T> Task.Then(Func<Task<T>>)

        [Fact]
        public Task Then_WithInputValue_WithTaskReturnValue_CallsContinuation()
        {
            // Arrange
            return TaskHelpers.FromResult(21)

            // Act
                              .Then(result =>
                              {
                                  return TaskHelpers.FromResult(42);
                              })

            // Assert
                              .ContinueWith(task =>
                              {
                                  Assert.Equal(TaskStatus.RanToCompletion, task.Status);
                                  Assert.Equal(42, task.Result);
                              });
        }

        [Fact]
        public Task Then_WithInputValue_WithTaskReturnValue_ThrownExceptionIsPropagated()
        {
            // Arrange
            return TaskHelpers.FromResult(21)

            // Act
                              .Then(result =>
                              {
                                  throw new NotImplementedException();
                                  return TaskHelpers.FromResult(0);  // Return-after-throw to guarantee correct lambda signature
                              })

            // Assert
                              .ContinueWith(task =>
                              {
                                  Assert.Equal(TaskStatus.Faulted, task.Status);
                                  var ex = Assert.Single(task.Exception.InnerExceptions);
                                  Assert.IsType<NotImplementedException>(ex);
                              });
        }

        [Fact]
        public Task Then_WithInputValue_WithTaskReturnValue_FaultPreventsFurtherThenStatementsFromExecuting()
        {
            // Arrange
            bool ranContinuation = false;

            return TaskHelpers.FromError<int>(new NotImplementedException())

            // Act
                              .Then(result =>
                              {
                                  ranContinuation = true;
                                  return TaskHelpers.FromResult(42);
                              })

            // Assert
                              .ContinueWith(task =>
                              {
                                  var ex = task.Exception;  // Observe the exception
                                  Assert.False(ranContinuation);
                              });
        }

        [Fact]
        public Task Then_WithInputValue_WithTaskReturnValue_ManualCancellationPreventsFurtherThenStatementsFromExecuting()
        {
            // Arrange
            bool ranContinuation = false;

            return TaskHelpers.Canceled<int>()

            // Act
                              .Then(result =>
                              {
                                  ranContinuation = true;
                                  return TaskHelpers.FromResult(42);
                              })

            // Assert
                              .ContinueWith(task =>
                              {
                                  Assert.Equal(TaskStatus.Canceled, task.Status);
                                  Assert.False(ranContinuation);
                              });
        }

        [Fact]
        public Task Then_WithInputValue_WithTaskReturnValue_TokenCancellationPreventsFurtherThenStatementsFromExecuting()
        {
            // Arrange
            bool ranContinuation = false;
            CancellationToken cancellationToken = new CancellationToken(canceled: true);

            return TaskHelpers.FromResult(21)

            // Act
                              .Then(result =>
                              {
                                  ranContinuation = true;
                                  return TaskHelpers.FromResult(42);
                              }, cancellationToken)

            // Assert
                              .ContinueWith(task =>
                              {
                                  Assert.Equal(TaskStatus.Canceled, task.Status);
                                  Assert.False(ranContinuation);
                              });
        }

        [Fact, PreserveSyncContext]
        public Task Then_WithInputValue_WithTaskReturnValue_IncompleteTask_RunsOnNewThreadAndPostsContinuationToSynchronizationContext()
        {
            // Arrange
            int originalThreadId = Thread.CurrentThread.ManagedThreadId;
            int callbackThreadId = Int32.MinValue;
            var syncContext = new Mock<SynchronizationContext> { CallBase = true };
            SynchronizationContext.SetSynchronizationContext(syncContext.Object);

            Task<int> incompleteTask = new Task<int>(() => 21);

            // Act
            Task resultTask = incompleteTask.Then(result =>
            {
                callbackThreadId = Thread.CurrentThread.ManagedThreadId;
                return TaskHelpers.FromResult(42);
            });

            // Assert
            incompleteTask.Start();

            return resultTask.ContinueWith(task =>
            {
                Assert.NotEqual(originalThreadId, callbackThreadId);
                syncContext.Verify(sc => sc.Post(It.IsAny<SendOrPostCallback>(), null), Times.Once());
            });
        }

        [Fact, PreserveSyncContext]
        public Task Then_WithInputValue_WithTaskReturnValue_CompleteTask_RunsOnSameThreadAndDoesNotPostToSynchronizationContext()
        {
            // Arrange
            int originalThreadId = Thread.CurrentThread.ManagedThreadId;
            int callbackThreadId = Int32.MinValue;
            var syncContext = new Mock<SynchronizationContext> { CallBase = true };
            SynchronizationContext.SetSynchronizationContext(syncContext.Object);

            return TaskHelpers.FromResult(21)

            // Act
                              .Then(result =>
                              {
                                  callbackThreadId = Thread.CurrentThread.ManagedThreadId;
                                  return TaskHelpers.FromResult(42);
                              })

            // Assert
                              .ContinueWith(task =>
                              {
                                  Assert.Equal(originalThreadId, callbackThreadId);
                                  syncContext.Verify(sc => sc.Post(It.IsAny<SendOrPostCallback>(), null), Times.Never());
                              });
        }

        // -----------------------------------------------------------------
        //  bool Task.TryGetResult(Task<TResult>, out TResult)

        [Fact]
        public void TryGetResult_CompleteTask_ReturnsTrueAndGivesResult()
        {
            // Arrange
            var task = TaskHelpers.FromResult(42);

            // Act
            int value;
            bool result = task.TryGetResult(out value);

            // Assert
            Assert.True(result);
            Assert.Equal(42, value);
        }

        [Fact]
        public void TryGetResult_FaultedTask_ReturnsFalse()
        {
            // Arrange
            var task = TaskHelpers.FromError<int>(new Exception());

            // Act
            int value;
            bool result = task.TryGetResult(out value);

            // Assert
            Assert.False(result);
            var ex = task.Exception; // Observe the task exception
        }

        [Fact]
        public void TryGetResult_CanceledTask_ReturnsFalse()
        {
            // Arrange
            var task = TaskHelpers.Canceled<int>();

            // Act
            int value;
            bool result = task.TryGetResult(out value);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public Task TryGetResult_IncompleteTask_ReturnsFalse()
        {
            // Arrange
            var incompleteTask = new Task<int>(() => 42);

            // Act
            int value;
            bool result = incompleteTask.TryGetResult(out value);

            // Assert
            Assert.False(result);

            incompleteTask.Start();
            return incompleteTask;  // Make sure the task gets observed
        }
    }
}