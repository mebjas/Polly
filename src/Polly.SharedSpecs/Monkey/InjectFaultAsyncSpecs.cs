﻿using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Polly.Monkey;
using Polly.Utilities;
using Xunit;

namespace Polly.Specs.Monkey
{
    [Collection(Polly.Specs.Helpers.Constants.SystemClockDependentTestCollection)]
    public class InjectFaultAsyncSpecs : IDisposable
    {
        public void Dispose()
        {
            RandomGenerator.Reset();
        }

        public void Init()
        {
            RandomGenerator.GetRandomNumber = () => 0.5;
        }

        #region Basic Overload, Exception, Context Free
        [Fact]
        public void InjectFault_Context_Free_Enabled_Should_not_execute_user_delegate_async()
        {
            this.Init();
            string exceptionMessage = "exceptionMessage";
            Exception fault = new Exception(exceptionMessage);
            MonkeyPolicy policy = Policy.InjectFaultAsync(fault, 0.6, () => true);
            Boolean executed = false;
            Func<Task> actionAsync = () => { executed = true; return TaskHelper.EmptyTask; };
            policy.Awaiting(async x => await x.ExecuteAsync(actionAsync)).ShouldThrowExactly<Exception>().WithMessage(exceptionMessage);
            executed.Should().BeFalse();
        }

        [Fact]
        public void InjectFault_Context_Free_Enabled_Should_execute_user_delegate_async()
        {
            this.Init();
            Exception fault = new Exception();
            MonkeyPolicy policy = Policy.InjectFaultAsync(fault, 0.3, () => true);
            Boolean executed = false;
            Func<Task> actionAsync = () => { executed = true; return TaskHelper.EmptyTask; };
            policy.Awaiting(async x => await x.ExecuteAsync(actionAsync)).ShouldNotThrow<Exception>();
            executed.Should().BeTrue();
        }
        #endregion

        #region Basic Overload, Exception, With Context
        [Fact]
        public void InjectFault_With_Context_Should_not_execute_user_delegate_async()
        {
            this.Init();
            Boolean executed = false;
            Context context = new Context();
            context["ShouldFail"] = "true";
            MonkeyPolicy policy = Policy.InjectFaultAsync(
                new Exception("test"),
                0.6,
                async (ctx) =>
                {
                    return await Task.FromResult((ctx["ShouldFail"] != null && ctx["ShouldFail"].ToString() == "true"));
                });

            Func<Context, Task> actionAsync = (_) => { executed = true; return TaskHelper.EmptyTask; };
            policy.Awaiting(async x => await x.ExecuteAsync(actionAsync, context)).ShouldThrowExactly<Exception>();

            executed.Should().BeFalse();
        }

        [Fact]
        public void InjectFault_With_Context_Should_execute_user_delegate_async()
        {
            this.Init();
            Boolean executed = false;
            Context context = new Context();
            context["ShouldFail"] = "true";
            MonkeyPolicy policy = Policy.InjectFaultAsync(
                new Exception("test"),
                0.4,
                async (ctx) =>
                {
                    return await Task.FromResult((ctx["ShouldFail"] != null && ctx["ShouldFail"].ToString() == "true"));
                });

            Func<Context, Task> actionAsync = (_) => { executed = true; return TaskHelper.EmptyTask; };
            policy.Awaiting(async x => await x.ExecuteAsync(actionAsync, context)).ShouldNotThrow<Exception>();
            executed.Should().BeTrue();
        }

        [Fact]
        public void InjectFault_With_Context_Should_execute_user_delegate_async_2()
        {
            this.Init();
            Boolean executed = false;
            Context context = new Context();
            context["ShouldFail"] = "false";
            MonkeyPolicy policy = Policy.InjectFaultAsync(
                new Exception("test"),
                0.6,
                async (ctx) =>
                {
                    return await Task.FromResult((ctx["ShouldFail"] != null && ctx["ShouldFail"].ToString() == "true"));
                });

            Func<Context, Task> actionAsync = (_) => { executed = true; return TaskHelper.EmptyTask; };
            policy.Awaiting(async x => await x.ExecuteAsync(actionAsync, context)).ShouldNotThrow<Exception>();
            executed.Should().BeTrue();
        }
        #endregion

        #region Overload, All based on context
        [Fact]
        public void InjectFault_With_Context_Should_not_execute_user_delegate_async_1()
        {
            this.Init();
            Boolean executed = false;
            Context context = new Context();
            context["ShouldFail"] = "true";

            Func<Context, CancellationToken, Task<Exception>> fault = (ctx, cts) => Task.FromResult(new Exception());
            Func<Context, Task<bool>> enabled = (ctx) => Task.FromResult(true);
            Func<Context, Task<Double>> injectionRate = (ctx) => Task.FromResult(0.6);
            MonkeyPolicy policy = Policy.InjectFaultAsync(fault, injectionRate, enabled);

            Func<Context, Task> actionAsync = (_) => { executed = true; return TaskHelper.EmptyTask; };
            policy.Awaiting(async x => await x.ExecuteAsync(actionAsync, context)).ShouldThrowExactly<Exception>();
            executed.Should().BeFalse();
        }

        [Fact]
        public void InjectFault_With_Context_Should_not_execute_user_delegate_async_2()
        {
            this.Init();
            Boolean executed = false;
            Context context = new Context();
            string failureMessage = "Failure Message";
            context["ShouldFail"] = "true";
            context["Message"] = failureMessage;
            context["InjectionRate"] = "0.6";

            Func<Context, CancellationToken, Task<Exception>> fault = (ctx, cts) =>
            {
                if (ctx["Message"] != null)
                {
                    Exception ex = new InvalidOperationException(ctx["Message"].ToString());
                    return Task.FromResult(ex);
                }

                return Task.FromResult(new Exception());
            };

            Func<Context, Task<Double>> injectionRate = (ctx) =>
            {
                double rate = 0;
                if (ctx["InjectionRate"] != null)
                {
                    rate = double.Parse(ctx["InjectionRate"].ToString());
                }

                return Task.FromResult(rate);
            };

            Func<Context, Task<bool>> enabled = (ctx) =>
            {
                return Task.FromResult(ctx["ShouldFail"] != null && ctx["ShouldFail"].ToString() == "true");
            };

            MonkeyPolicy policy = Policy.InjectFaultAsync(fault, injectionRate, enabled);
            Func<Context, Task> actionAsync = (_) => { executed = true; return TaskHelper.EmptyTask; };
            policy.Awaiting(async x => await x.ExecuteAsync(actionAsync, context))
                .ShouldThrowExactly<InvalidOperationException>()
                .WithMessage(failureMessage);
            executed.Should().BeFalse();
        }
        #endregion
    }
}
