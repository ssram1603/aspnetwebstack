﻿using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Json;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
using System.Web.Http.Routing;
using System.Web.Http.ValueProviders;
using Microsoft.TestCommon;
using Xunit;

namespace System.Web.Http.ModelBinding
{
    public class DefaultActionValueBinderTest
    {
        [Fact]
        public void BindValuesAsync_Uses_DefaultValues()
        {
            // Arrange
            HttpActionContext context = ContextUtil.CreateActionContext(
                ContextUtil.CreateControllerContext(),
                new ReflectedHttpActionDescriptor() { MethodInfo = typeof(ActionValueController).GetMethod("Get") });
            CancellationToken cancellationToken = new CancellationToken();
            DefaultActionValueBinder provider = new DefaultActionValueBinder();

            // Act
            provider.BindValuesAsync(context, cancellationToken).Wait();

            // Assert
            Dictionary<string, object> expectedResult = new Dictionary<string, object>();
            expectedResult["id"] = 0;
            expectedResult["firstName"] = "DefaultFirstName";
            expectedResult["lastName"] = "DefaultLastName";
            Assert.Equal(expectedResult, context.ActionArguments, new DictionaryEqualityComparer());
        }

        [Fact]
        public void BindValuesAsync_WithObjectContentInRequest_Works()
        {
            // Arrange
            ActionValueItem cust = new ActionValueItem() { FirstName = "FirstName", LastName = "LastName", Id = 1 };
            HttpActionContext context = ContextUtil.CreateActionContext(
                ContextUtil.CreateControllerContext(),
                new ReflectedHttpActionDescriptor() { MethodInfo = typeof(ActionValueController).GetMethod("PostComplexType") });
            context.ControllerContext.Request = new HttpRequestMessage
            {
                Content = new ObjectContent<ActionValueItem>(cust, new JsonMediaTypeFormatter())
            };
            CancellationToken cancellationToken = new CancellationToken();
            DefaultActionValueBinder provider = new DefaultActionValueBinder();

            // Act
            provider.BindValuesAsync(context, cancellationToken).Wait();

            // Assert
            Dictionary<string, object> expectedResult = new Dictionary<string, object>();
            expectedResult["item"] = cust;
            Assert.Equal(expectedResult, context.ActionArguments, new DictionaryEqualityComparer());
        }

        #region Query Strings

        [Fact]
        public void BindValuesAsync_Query_String_Values_To_Simple_Types()
        {
            // Arrange
            CancellationToken cancellationToken = new CancellationToken();
            HttpActionContext actionContext = ContextUtil.CreateActionContext(
                ContextUtil.CreateControllerContext(new HttpRequestMessage()
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri("http://localhost?id=5&firstName=queryFirstName&lastName=queryLastName")
                }),
                new ReflectedHttpActionDescriptor() { MethodInfo = typeof(ActionValueController).GetMethod("Get") });

            DefaultActionValueBinder provider = new DefaultActionValueBinder();

            // Act
            provider.BindValuesAsync(actionContext, cancellationToken).Wait();

            // Assert
            Dictionary<string, object> expectedResult = new Dictionary<string, object>();
            expectedResult["id"] = 5;
            expectedResult["firstName"] = "queryFirstName";
            expectedResult["lastName"] = "queryLastName";
            Assert.Equal(expectedResult, actionContext.ActionArguments, new DictionaryEqualityComparer());
        }

        [Fact]
        public void BindValuesAsync_Query_String_Values_To_Simple_Types_With_FromUriAttribute()
        {
            // Arrange
            CancellationToken cancellationToken = new CancellationToken();
            HttpActionContext actionContext = ContextUtil.CreateActionContext(
                ContextUtil.CreateControllerContext(new HttpRequestMessage()
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri("http://localhost?id=5&firstName=queryFirstName&lastName=queryLastName")
                }),
                new ReflectedHttpActionDescriptor() { MethodInfo = typeof(ActionValueController).GetMethod("GetFromUri") });

            DefaultActionValueBinder provider = new DefaultActionValueBinder();

            // Act
            provider.BindValuesAsync(actionContext, cancellationToken).Wait();

            // Assert
            Dictionary<string, object> expectedResult = new Dictionary<string, object>();
            expectedResult["id"] = 5;
            expectedResult["firstName"] = "queryFirstName";
            expectedResult["lastName"] = "queryLastName";
            Assert.Equal(expectedResult, actionContext.ActionArguments, new DictionaryEqualityComparer());
        }

        [Fact]
        public void BindValuesAsync_Query_String_Values_To_Complex_Types()
        {
            // Arrange
            CancellationToken cancellationToken = new CancellationToken();
            HttpActionContext actionContext = ContextUtil.CreateActionContext(
                ContextUtil.CreateControllerContext(new HttpRequestMessage()
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri("http://localhost?id=5&firstName=queryFirstName&lastName=queryLastName")
                }),
                new ReflectedHttpActionDescriptor() { MethodInfo = typeof(ActionValueController).GetMethod("GetItem") });

            DefaultActionValueBinder provider = new DefaultActionValueBinder();

            // Act
            provider.BindValuesAsync(actionContext, cancellationToken).Wait();

            // Assert
            Assert.True(actionContext.ModelState.IsValid);
            Assert.Equal(1, actionContext.ActionArguments.Count);
            ActionValueItem deserializedActionValueItem = Assert.IsType<ActionValueItem>(actionContext.ActionArguments.First().Value);
            Assert.Equal(5, deserializedActionValueItem.Id);
            Assert.Equal("queryFirstName", deserializedActionValueItem.FirstName);
            Assert.Equal("queryLastName", deserializedActionValueItem.LastName);
        }

        [Fact]
        public void BindValuesAsync_Query_String_Values_To_Post_Complex_Types()
        {
            // Arrange
            CancellationToken cancellationToken = new CancellationToken();
            HttpActionContext actionContext = ContextUtil.CreateActionContext(
                ContextUtil.CreateControllerContext(new HttpRequestMessage()
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri("http://localhost?id=5&firstName=queryFirstName&lastName=queryLastName")
                }),
                new ReflectedHttpActionDescriptor() { MethodInfo = typeof(ActionValueController).GetMethod("PostComplexTypeUri") });

            DefaultActionValueBinder provider = new DefaultActionValueBinder();

            // Act
            provider.BindValuesAsync(actionContext, cancellationToken).Wait();

            // Assert
            Assert.True(actionContext.ModelState.IsValid);
            Assert.Equal(1, actionContext.ActionArguments.Count);
            ActionValueItem deserializedActionValueItem = Assert.IsType<ActionValueItem>(actionContext.ActionArguments.First().Value);
            Assert.Equal(5, deserializedActionValueItem.Id);
            Assert.Equal("queryFirstName", deserializedActionValueItem.FirstName);
            Assert.Equal("queryLastName", deserializedActionValueItem.LastName);
        }

        [Fact]
        public void BindValuesAsync_Query_String_Values_To_Post_Enumerable_Complex_Types()
        {
            // Arrange
            CancellationToken cancellationToken = new CancellationToken();
            HttpActionContext actionContext = ContextUtil.CreateActionContext(
                ContextUtil.CreateControllerContext(new HttpRequestMessage()
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri("http://localhost?items[0].id=5&items[0].firstName=queryFirstName&items[0].lastName=queryLastName")
                }),
                new ReflectedHttpActionDescriptor() { MethodInfo = typeof(ActionValueController).GetMethod("PostEnumerableUri") });

            DefaultActionValueBinder provider = new DefaultActionValueBinder();

            // Act
            provider.BindValuesAsync(actionContext, cancellationToken).Wait();

            // Assert
            Assert.True(actionContext.ModelState.IsValid);
            Assert.Equal(1, actionContext.ActionArguments.Count);
            IEnumerable<ActionValueItem> items = Assert.IsAssignableFrom<IEnumerable<ActionValueItem>>(actionContext.ActionArguments.First().Value);
            ActionValueItem deserializedActionValueItem = items.First();
            Assert.Equal(5, deserializedActionValueItem.Id);
            Assert.Equal("queryFirstName", deserializedActionValueItem.FirstName);
            Assert.Equal("queryLastName", deserializedActionValueItem.LastName);
        }

        [Fact]
        public void BindValuesAsync_Query_String_Values_To_Post_Enumerable_Complex_Types_No_Index()
        {
            // Arrange
            CancellationToken cancellationToken = new CancellationToken();
            HttpActionContext actionContext = ContextUtil.CreateActionContext(
                ContextUtil.CreateControllerContext(new HttpRequestMessage()
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri("http://localhost?id=5&firstName=queryFirstName&items.lastName=queryLastName")
                }),
                new ReflectedHttpActionDescriptor() { MethodInfo = typeof(ActionValueController).GetMethod("PostEnumerableUri") });

            DefaultActionValueBinder provider = new DefaultActionValueBinder();

            // Act
            provider.BindValuesAsync(actionContext, cancellationToken).Wait();

            // Assert
            Assert.True(actionContext.ModelState.IsValid);
            Assert.Equal(1, actionContext.ActionArguments.Count);
            IEnumerable<ActionValueItem> items = Assert.IsAssignableFrom<IEnumerable<ActionValueItem>>(actionContext.ActionArguments.First().Value);
            Assert.Equal(0, items.Count());     // expect unsuccessful bind but proves we don't loop infinitely
        }

        [Fact]
        public void BindValuesAsync_Query_String_Values_To_ComplexType_Using_Prefixes()
        {
            // Arrange
            CancellationToken cancellationToken = new CancellationToken();
            HttpActionContext actionContext = ContextUtil.CreateActionContext(
                ContextUtil.CreateControllerContext(new HttpRequestMessage()
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri("http://localhost?item.id=5&item.firstName=queryFirstName&item.lastName=queryLastName")
                }),
                new ReflectedHttpActionDescriptor() { MethodInfo = typeof(ActionValueController).GetMethod("GetItem") });

            DefaultActionValueBinder provider = new DefaultActionValueBinder();

            // Act
            provider.BindValuesAsync(actionContext, cancellationToken).Wait();

            // Assert
            Assert.Equal(1, actionContext.ActionArguments.Count);
            ActionValueItem deserializedActionValueItem = Assert.IsType<ActionValueItem>(actionContext.ActionArguments.First().Value);
            Assert.Equal(5, deserializedActionValueItem.Id);
            Assert.Equal("queryFirstName", deserializedActionValueItem.FirstName);
            Assert.Equal("queryLastName", deserializedActionValueItem.LastName);
        }

        [Fact]
        public void BindValuesAsync_Query_String_Values_To_ComplexType_Using_FromUriAttribute()
        {
            // Arrange
            CancellationToken cancellationToken = new CancellationToken();
            HttpActionContext actionContext = ContextUtil.CreateActionContext(
                ContextUtil.CreateControllerContext(new HttpRequestMessage()
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri("http://localhost?item.id=5&item.firstName=queryFirstName&item.lastName=queryLastName")
                }),
                new ReflectedHttpActionDescriptor() { MethodInfo = typeof(ActionValueController).GetMethod("GetItemFromUri") });

            DefaultActionValueBinder provider = new DefaultActionValueBinder();

            // Act
            provider.BindValuesAsync(actionContext, cancellationToken).Wait();

            // Assert
            Assert.Equal(1, actionContext.ActionArguments.Count);
            ActionValueItem deserializedActionValueItem = Assert.IsType<ActionValueItem>(actionContext.ActionArguments.First().Value);
            Assert.Equal(5, deserializedActionValueItem.Id);
            Assert.Equal("queryFirstName", deserializedActionValueItem.FirstName);
            Assert.Equal("queryLastName", deserializedActionValueItem.LastName);
        }

        [Fact]
        public void BindValuesAsync_Query_String_Values_Using_Custom_ValueProviderAttribute()
        {
            // Arrange
            CancellationToken cancellationToken = new CancellationToken();
            HttpActionContext actionContext = ContextUtil.CreateActionContext(
                ContextUtil.CreateControllerContext(new HttpRequestMessage()
                {
                    Method = HttpMethod.Get
                }),
                new ReflectedHttpActionDescriptor() { MethodInfo = typeof(ActionValueController).GetMethod("GetFromCustom") });

            DefaultActionValueBinder provider = new DefaultActionValueBinder();

            // Act
            provider.BindValuesAsync(actionContext, cancellationToken).Wait();

            // Assert
            Dictionary<string, object> expectedResult = new Dictionary<string, object>();
            expectedResult["id"] = 99;
            expectedResult["firstName"] = "99";
            expectedResult["lastName"] = "99";
            Assert.Equal(expectedResult, actionContext.ActionArguments, new DictionaryEqualityComparer());
        }

        [Fact]
        public void BindValuesAsync_Query_String_Values_Using_Prefix_To_Rename()
        {
            // Arrange
            CancellationToken cancellationToken = new CancellationToken();
            HttpActionContext actionContext = ContextUtil.CreateActionContext(
                ContextUtil.CreateControllerContext(new HttpRequestMessage()
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri("http://localhost?custid=5&first=renamedFirstName&last=renamedLastName")
                    // notice the query string names match the prefixes in GetFromNamed() and not the actual parameter names
                }),
                new ReflectedHttpActionDescriptor() { MethodInfo = typeof(ActionValueController).GetMethod("GetFromNamed") });

            DefaultActionValueBinder provider = new DefaultActionValueBinder();

            // Act
            provider.BindValuesAsync(actionContext, cancellationToken).Wait();

            // Assert
            Dictionary<string, object> expectedResult = new Dictionary<string, object>();
            expectedResult["id"] = 5;
            expectedResult["firstName"] = "renamedFirstName";
            expectedResult["lastName"] = "renamedLastName";
            Assert.Equal(expectedResult, actionContext.ActionArguments, new DictionaryEqualityComparer());
        }

        [Fact]
        public void BindValuesAsync_Query_String_Values_To_Complex_Types_With_Validation_Error()
        {
            // Arrange
            CancellationToken cancellationToken = new CancellationToken();
            HttpActionContext actionContext = ContextUtil.CreateActionContext(
                ContextUtil.CreateControllerContext(new HttpRequestMessage()
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri("http://localhost?id=100&firstName=queryFirstName&lastName=queryLastName")
                }),
                new ReflectedHttpActionDescriptor() { MethodInfo = typeof(ActionValueController).GetMethod("GetItem") });

            DefaultActionValueBinder provider = new DefaultActionValueBinder();

            // Act
            provider.BindValuesAsync(actionContext, cancellationToken).Wait();

            // Assert
            Assert.False(actionContext.ModelState.IsValid);
        }

        #endregion Query Strings

        #region RouteData

        [Fact]
        public void BindValuesAsync_RouteData_Values_To_Simple_Types()
        {
            // Arrange
            CancellationToken cancellationToken = new CancellationToken();
            HttpRouteData route = new HttpRouteData(new HttpRoute());
            route.Values.Add("id", 6);
            route.Values.Add("firstName", "routeFirstName");
            route.Values.Add("lastName", "routeLastName");

            HttpActionContext controllerContext = ContextUtil.CreateActionContext(
                ContextUtil.CreateControllerContext(route, new HttpRequestMessage()
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri("http://localhost")
                }),
                new ReflectedHttpActionDescriptor() { MethodInfo = typeof(ActionValueController).GetMethod("Get") });

            DefaultActionValueBinder provider = new DefaultActionValueBinder();

            // Act
            provider.BindValuesAsync(controllerContext, cancellationToken).Wait();

            // Assert
            Dictionary<string, object> expectedResult = new Dictionary<string, object>();
            expectedResult["id"] = 6;
            expectedResult["firstName"] = "routeFirstName";
            expectedResult["lastName"] = "routeLastName";
            Assert.Equal(expectedResult, controllerContext.ActionArguments, new DictionaryEqualityComparer());
        }

        [Fact]
        public void BindValuesAsync_RouteData_Values_To_Simple_Types_Using_FromUriAttribute()
        {
            // Arrange
            CancellationToken cancellationToken = new CancellationToken();
            HttpRouteData route = new HttpRouteData(new HttpRoute());
            route.Values.Add("id", 6);
            route.Values.Add("firstName", "routeFirstName");
            route.Values.Add("lastName", "routeLastName");

            HttpActionContext controllerContext = ContextUtil.CreateActionContext(
                ContextUtil.CreateControllerContext(route, new HttpRequestMessage()
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri("http://localhost")
                }),
                new ReflectedHttpActionDescriptor() { MethodInfo = typeof(ActionValueController).GetMethod("Get") });

            DefaultActionValueBinder provider = new DefaultActionValueBinder();

            // Act
            provider.BindValuesAsync(controllerContext, cancellationToken).Wait();

            // Assert
            Dictionary<string, object> expectedResult = new Dictionary<string, object>();
            expectedResult["id"] = 6;
            expectedResult["firstName"] = "routeFirstName";
            expectedResult["lastName"] = "routeLastName";
            Assert.Equal(expectedResult, controllerContext.ActionArguments, new DictionaryEqualityComparer());
        }

        [Fact]
        public void BindValuesAsync_RouteData_Values_To_Complex_Types()
        {
            // Arrange
            CancellationToken cancellationToken = new CancellationToken();
            HttpRouteData route = new HttpRouteData(new HttpRoute());
            route.Values.Add("id", 6);
            route.Values.Add("firstName", "routeFirstName");
            route.Values.Add("lastName", "routeLastName");

            HttpActionContext controllerContext = ContextUtil.CreateActionContext(
                ContextUtil.CreateControllerContext(route, new HttpRequestMessage()
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri("http://localhost")
                }),
                new ReflectedHttpActionDescriptor() { MethodInfo = typeof(ActionValueController).GetMethod("GetItem") });

            DefaultActionValueBinder provider = new DefaultActionValueBinder();

            // Act
            provider.BindValuesAsync(controllerContext, cancellationToken).Wait();

            // Assert
            Assert.Equal(1, controllerContext.ActionArguments.Count);
            ActionValueItem deserializedActionValueItem = Assert.IsType<ActionValueItem>(controllerContext.ActionArguments.First().Value);
            Assert.Equal(6, deserializedActionValueItem.Id);
            Assert.Equal("routeFirstName", deserializedActionValueItem.FirstName);
            Assert.Equal("routeLastName", deserializedActionValueItem.LastName);
        }

        [Fact]
        public void BindValuesAsync_RouteData_Values_To_Complex_Types_Using_FromUriAttribute()
        {
            // Arrange
            CancellationToken cancellationToken = new CancellationToken();
            HttpRouteData route = new HttpRouteData(new HttpRoute());
            route.Values.Add("id", 6);
            route.Values.Add("firstName", "routeFirstName");
            route.Values.Add("lastName", "routeLastName");

            HttpActionContext controllerContext = ContextUtil.CreateActionContext(
                ContextUtil.CreateControllerContext(route, new HttpRequestMessage()
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri("http://localhost")
                }),
                new ReflectedHttpActionDescriptor() { MethodInfo = typeof(ActionValueController).GetMethod("GetItemFromUri") });

            DefaultActionValueBinder provider = new DefaultActionValueBinder();

            // Act
            provider.BindValuesAsync(controllerContext, cancellationToken).Wait();

            // Assert
            Assert.Equal(1, controllerContext.ActionArguments.Count);
            ActionValueItem deserializedActionValueItem = Assert.IsType<ActionValueItem>(controllerContext.ActionArguments.First().Value);
            Assert.Equal(6, deserializedActionValueItem.Id);
            Assert.Equal("routeFirstName", deserializedActionValueItem.FirstName);
            Assert.Equal("routeLastName", deserializedActionValueItem.LastName);
        }

        #endregion RouteData

        #region ControllerContext
        [Fact]
        public void BindValuesAsync_ControllerContext_CancellationToken()
        {
            // Arrange
            CancellationToken cancellationToken = new CancellationToken();
            HttpActionContext actionContext = ContextUtil.CreateActionContext(
                ContextUtil.CreateControllerContext(new HttpRequestMessage()
                {
                    Method = HttpMethod.Get
                }),
                new ReflectedHttpActionDescriptor() { MethodInfo = typeof(ActionValueController).GetMethod("GetFromCancellationToken") });

            DefaultActionValueBinder provider = new DefaultActionValueBinder();

            // Act
            provider.BindValuesAsync(actionContext, cancellationToken).Wait();

            // Assert
            Assert.Equal(1, actionContext.ActionArguments.Count);
            Assert.Equal(cancellationToken, actionContext.ActionArguments.First().Value);
        }
        #endregion ControllerContext

        #region Body

        [Fact]
        public void BindValuesAsync_Body_To_Complex_Type_Json()
        {
            // Arrange
            CancellationToken cancellationToken = new CancellationToken();
            string jsonString = "{\"Id\":\"7\",\"FirstName\":\"testFirstName\",\"LastName\":\"testLastName\"}";
            StringContent stringContent = new StringContent(jsonString, Encoding.UTF8, "application/json");

            HttpRequestMessage request = new HttpRequestMessage() { Content = stringContent };
            HttpActionContext context = ContextUtil.CreateActionContext(
                ContextUtil.CreateControllerContext(request),
                new ReflectedHttpActionDescriptor() { MethodInfo = typeof(ActionValueController).GetMethod("PostComplexType") });

            DefaultActionValueBinder provider = new DefaultActionValueBinder();

            // Act
            provider.BindValuesAsync(context, cancellationToken).Wait();

            // Assert
            Assert.Equal(1, context.ActionArguments.Count);
            ActionValueItem deserializedActionValueItem = Assert.IsAssignableFrom<ActionValueItem>(context.ActionArguments.First().Value);
            Assert.Equal(7, deserializedActionValueItem.Id);
            Assert.Equal("testFirstName", deserializedActionValueItem.FirstName);
            Assert.Equal("testLastName", deserializedActionValueItem.LastName);
        }

        [Fact]
        public void BindValuesAsync_Body_To_Complex_Type_Json_With_Validation_Error()
        {
            // Arrange
            CancellationToken cancellationToken = new CancellationToken();
            string jsonString = "{\"Id\":\"100\",\"FirstName\":\"testFirstName\",\"LastName\":\"testLastName\"}";
            StringContent stringContent = new StringContent(jsonString, Encoding.UTF8, "application/json");

            HttpRequestMessage request = new HttpRequestMessage() { Content = stringContent };
            HttpActionContext context = ContextUtil.CreateActionContext(
                ContextUtil.CreateControllerContext(request),
                new ReflectedHttpActionDescriptor() { MethodInfo = typeof(ActionValueController).GetMethod("PostComplexType") });

            DefaultActionValueBinder provider = new DefaultActionValueBinder();

            // Act
            provider.BindValuesAsync(context, cancellationToken).Wait();

            // Assert
            Assert.False(context.ModelState.IsValid);
        }

        [Fact]
        public void BindValuesAsync_Body_To_Complex_Type_FormUrlEncoded()
        {
            // Arrange
            CancellationToken cancellationToken = new CancellationToken();
            string formUrlEncodedString = "Id=7&FirstName=testFirstName&LastName=testLastName";
            StringContent stringContent = new StringContent(formUrlEncodedString, Encoding.UTF8, "application/x-www-form-urlencoded");

            HttpRequestMessage request = new HttpRequestMessage() { Content = stringContent };
            HttpActionContext context = ContextUtil.CreateActionContext(
                ContextUtil.CreateControllerContext(request),
                new ReflectedHttpActionDescriptor() { MethodInfo = typeof(ActionValueController).GetMethod("PostComplexType") });

            DefaultActionValueBinder provider = new DefaultActionValueBinder();

            // Act
            provider.BindValuesAsync(context, cancellationToken).Wait();

            // Assert
            Assert.Equal(1, context.ActionArguments.Count);
            ActionValueItem deserializedActionValueItem = Assert.IsAssignableFrom<ActionValueItem>(context.ActionArguments.First().Value);
            Assert.Equal(7, deserializedActionValueItem.Id);
            Assert.Equal("testFirstName", deserializedActionValueItem.FirstName);
            Assert.Equal("testLastName", deserializedActionValueItem.LastName);
        }

        [Fact]
        public void BindValuesAsync_Body_To_Complex_Type_FormUrlEncoded_With_Validation_Error()
        {
            // Arrange
            CancellationToken cancellationToken = new CancellationToken();
            string formUrlEncodedString = "Id=101&FirstName=testFirstName&LastName=testLastName";
            StringContent stringContent = new StringContent(formUrlEncodedString, Encoding.UTF8, "application/x-www-form-urlencoded");

            HttpRequestMessage request = new HttpRequestMessage() { Content = stringContent };
            HttpActionContext context = ContextUtil.CreateActionContext(
                ContextUtil.CreateControllerContext(request),
                new ReflectedHttpActionDescriptor() { MethodInfo = typeof(ActionValueController).GetMethod("PostComplexType") });

            DefaultActionValueBinder provider = new DefaultActionValueBinder();

            // Act
            provider.BindValuesAsync(context, cancellationToken).Wait();

            // Assert
            Assert.False(context.ModelState.IsValid);
        }

        [Fact]
        public void BindValuesAsync_Body_To_Complex_Type_Xml()
        {
            // Arrange
            CancellationToken cancellationToken = new CancellationToken();
            MediaTypeHeaderValue mediaType = new MediaTypeHeaderValue("application/xml");
            ActionValueItem item = new ActionValueItem() { Id = 7, FirstName = "testFirstName", LastName = "testLastName" };
            ObjectContent<ActionValueItem> tempContent = new ObjectContent<ActionValueItem>(item, new XmlMediaTypeFormatter());
            StringContent stringContent = new StringContent(tempContent.ReadAsStringAsync().Result);
            stringContent.Headers.ContentType = mediaType;
            HttpRequestMessage request = new HttpRequestMessage() { Content = stringContent };
            HttpActionContext context = ContextUtil.CreateActionContext(
                ContextUtil.CreateControllerContext(request),
                new ReflectedHttpActionDescriptor() { MethodInfo = typeof(ActionValueController).GetMethod("PostComplexType") });

            DefaultActionValueBinder provider = new DefaultActionValueBinder();

            // Act
            provider.BindValuesAsync(context, cancellationToken).Wait();

            // Assert
            Assert.Equal(1, context.ActionArguments.Count);
            ActionValueItem deserializedActionValueItem = Assert.IsAssignableFrom<ActionValueItem>(context.ActionArguments.First().Value);
            Assert.Equal(item.Id, deserializedActionValueItem.Id);
            Assert.Equal(item.FirstName, deserializedActionValueItem.FirstName);
            Assert.Equal(item.LastName, deserializedActionValueItem.LastName);
        }

        [Fact]
        public void BindValuesAsync_Body_To_Complex_Type_Xml_Structural()
        {
            // Arrange
            CancellationToken cancellationToken = new CancellationToken();
            MediaTypeHeaderValue mediaType = new MediaTypeHeaderValue("application/xml");

            // Test sending from a non .NET type (raw xml).            
            // The default XML serializer requires that the xml root name matches the C# class name. 
            string xmlSource =
                @"<?xml version='1.0' encoding='utf-8'?>
                <ActionValueItem xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xsd='http://www.w3.org/2001/XMLSchema'>
                    <Id>7</Id>
                    <FirstName>testFirstName</FirstName>
                    <LastName>testLastName</LastName>
                </ActionValueItem>".Replace('\'', '"');

            StringContent stringContent = new StringContent(xmlSource);
            stringContent.Headers.ContentType = mediaType;
            HttpRequestMessage request = new HttpRequestMessage() { Content = stringContent };
            HttpActionContext context = ContextUtil.CreateActionContext(
                ContextUtil.CreateControllerContext(request),
                new ReflectedHttpActionDescriptor() { MethodInfo = typeof(ActionValueController).GetMethod("PostComplexType") });

            DefaultActionValueBinder provider = new DefaultActionValueBinder();

            // Act
            provider.BindValuesAsync(context, cancellationToken).Wait();

            // Assert
            Assert.Equal(1, context.ActionArguments.Count);
            ActionValueItem deserializedActionValueItem = Assert.IsAssignableFrom<ActionValueItem>(context.ActionArguments.First().Value);
            Assert.Equal(7, deserializedActionValueItem.Id);
            Assert.Equal("testFirstName", deserializedActionValueItem.FirstName);
            Assert.Equal("testLastName", deserializedActionValueItem.LastName);
        }

        [Fact]
        public void BindValuesAsync_Body_To_Complex_Type_Xml_With_Validation_Error()
        {
            // Arrange
            CancellationToken cancellationToken = new CancellationToken();
            MediaTypeHeaderValue mediaType = new MediaTypeHeaderValue("application/xml");
            ActionValueItem item = new ActionValueItem() { Id = 101, FirstName = "testFirstName", LastName = "testLastName" };
            var tempContent = new ObjectContent<ActionValueItem>(item, new XmlMediaTypeFormatter());
            StringContent stringContent = new StringContent(tempContent.ReadAsStringAsync().Result);
            stringContent.Headers.ContentType = mediaType;
            HttpRequestMessage request = new HttpRequestMessage() { Content = stringContent };
            HttpActionContext context = ContextUtil.CreateActionContext(
                ContextUtil.CreateControllerContext(request),
                new ReflectedHttpActionDescriptor() { MethodInfo = typeof(ActionValueController).GetMethod("PostComplexType") });

            DefaultActionValueBinder provider = new DefaultActionValueBinder();

            // Act
            provider.BindValuesAsync(context, cancellationToken).Wait();

            // Assert
            Assert.False(context.ModelState.IsValid);
        }

        [Fact]
        public void BindValuesAsync_Body_To_Complex_And_Uri_To_Simple()
        {
            // Arrange
            string jsonString = "{\"Id\":\"7\",\"FirstName\":\"testFirstName\",\"LastName\":\"testLastName\"}";
            StringContent stringContent = new StringContent(jsonString, Encoding.UTF8, "application/json");

            HttpRequestMessage request = new HttpRequestMessage() 
            { 
                RequestUri = new Uri("http://localhost/ActionValueController/PostFromBody?id=123"),
                Content = stringContent 
            };

            HttpActionContext context = ContextUtil.CreateActionContext(
                ContextUtil.CreateControllerContext(request),
                new ReflectedHttpActionDescriptor() { MethodInfo = typeof(ActionValueController).GetMethod("PostFromBodyAndUri") });

            DefaultActionValueBinder provider = new DefaultActionValueBinder();

            // Act
            provider.BindValuesAsync(context, CancellationToken.None).Wait();

            // Assert
            Assert.Equal(2, context.ActionArguments.Count);
            Assert.Equal(123, context.ActionArguments["id"]);

            ActionValueItem deserializedActionValueItem = Assert.IsAssignableFrom<ActionValueItem>(context.ActionArguments["item"]);
            Assert.Equal(7, deserializedActionValueItem.Id);
            Assert.Equal("testFirstName", deserializedActionValueItem.FirstName);
            Assert.Equal("testLastName", deserializedActionValueItem.LastName);
        }

        [Fact]
        public void BindValuesAsync_Body_To_Complex_Type_Using_FromBodyAttribute()
        {
            // Arrange
            CancellationToken cancellationToken = new CancellationToken();
            string jsonString = "{\"Id\":\"7\",\"FirstName\":\"testFirstName\",\"LastName\":\"testLastName\"}";
            StringContent stringContent = new StringContent(jsonString, Encoding.UTF8, "application/json");

            HttpRequestMessage request = new HttpRequestMessage() { Content = stringContent };

            HttpActionContext context = ContextUtil.CreateActionContext(
                ContextUtil.CreateControllerContext(request),
                new ReflectedHttpActionDescriptor() { MethodInfo = typeof(ActionValueController).GetMethod("PostFromBody") });

            DefaultActionValueBinder provider = new DefaultActionValueBinder();

            // Act
            provider.BindValuesAsync(context, cancellationToken).Wait();

            // Assert
            Assert.Equal(1, context.ActionArguments.Count);
            ActionValueItem deserializedActionValueItem = Assert.IsAssignableFrom<ActionValueItem>(context.ActionArguments.First().Value);
            Assert.Equal(7, deserializedActionValueItem.Id);
            Assert.Equal("testFirstName", deserializedActionValueItem.FirstName);
            Assert.Equal("testLastName", deserializedActionValueItem.LastName);
        }

        [Fact]
        public void BindValuesAsync_Body_To_Complex_Type_Using_Formatter_To_Deserialize()
        {
            // Arrange
            CancellationToken cancellationToken = new CancellationToken();
            string jsonString = "{\"Id\":\"7\",\"FirstName\":\"testFirstName\",\"LastName\":\"testLastName\"}";
            StringContent stringContent = new StringContent(jsonString, Encoding.UTF8, "application/json");

            HttpRequestMessage request = new HttpRequestMessage() { Content = stringContent };
            HttpActionContext context = ContextUtil.CreateActionContext(
                ContextUtil.CreateControllerContext(request),
                new ReflectedHttpActionDescriptor() { MethodInfo = typeof(ActionValueController).GetMethod("PostComplexType") });
            DefaultActionValueBinder provider = new DefaultActionValueBinder();

            // Act
            provider.BindValuesAsync(context, cancellationToken).Wait();

            // Assert
            Assert.Equal(1, context.ActionArguments.Count);
            ActionValueItem deserializedActionValueItem = Assert.IsAssignableFrom<ActionValueItem>(context.ActionArguments.First().Value);
            Assert.Equal(7, deserializedActionValueItem.Id);
            Assert.Equal("testFirstName", deserializedActionValueItem.FirstName);
            Assert.Equal("testLastName", deserializedActionValueItem.LastName);
        }


        [Fact]
        public void BindValuesAsync_Body_To_IEnumerable_Complex_Type_Json()
        {
            // ModelBinding will bind T to IEnumerable<T>, but JSON.Net won't. So enclose JSON in [].
            // Arrange
            CancellationToken cancellationToken = new CancellationToken();
            string jsonString = "[{\"Id\":\"7\",\"FirstName\":\"testFirstName\",\"LastName\":\"testLastName\"}]";
            StringContent stringContent = new StringContent(jsonString, Encoding.UTF8, "application/json");

            HttpRequestMessage request = new HttpRequestMessage() { Content = stringContent };
            HttpActionContext context = ContextUtil.CreateActionContext(
                ContextUtil.CreateControllerContext(request),
                new ReflectedHttpActionDescriptor() { MethodInfo = typeof(ActionValueController).GetMethod("PostEnumerable") });

            DefaultActionValueBinder provider = new DefaultActionValueBinder();

            // Act
            provider.BindValuesAsync(context, cancellationToken).Wait();

            // Assert
            Assert.Equal(1, context.ActionArguments.Count);
            IEnumerable<ActionValueItem> items = Assert.IsAssignableFrom<IEnumerable<ActionValueItem>>(context.ActionArguments.First().Value);
            ActionValueItem deserializedActionValueItem = items.First();
            Assert.Equal(7, deserializedActionValueItem.Id);
            Assert.Equal("testFirstName", deserializedActionValueItem.FirstName);
            Assert.Equal("testLastName", deserializedActionValueItem.LastName);
        }

        [Fact]
        public void BindValuesAsync_Body_To_JsonValue()
        {
            // Arrange
            CancellationToken cancellationToken = new CancellationToken();
            MediaTypeHeaderValue mediaType = new MediaTypeHeaderValue("application/json");
            ActionValueItem item = new ActionValueItem() { Id = 7, FirstName = "testFirstName", LastName = "testLastName" };
            string json = "{\"a\":123,\"b\":[false,null,12.34]}";
            JsonValue jv = JsonValue.Parse(json);
            var tempContent = new ObjectContent<JsonValue>(jv, new JsonMediaTypeFormatter());
            StringContent stringContent = new StringContent(tempContent.ReadAsStringAsync().Result);
            stringContent.Headers.ContentType = mediaType;
            HttpRequestMessage request = new HttpRequestMessage() { Content = stringContent };
            HttpActionContext context = ContextUtil.CreateActionContext(
                ContextUtil.CreateControllerContext(request),
                new ReflectedHttpActionDescriptor() { MethodInfo = typeof(ActionValueController).GetMethod("PostJsonValue") });

            DefaultActionValueBinder provider = new DefaultActionValueBinder();

            // Act
            provider.BindValuesAsync(context, cancellationToken).Wait();

            // Assert
            Assert.Equal(1, context.ActionArguments.Count);
            JsonValue deserializedJsonValue = Assert.IsAssignableFrom<JsonValue>(context.ActionArguments.First().Value);
            string deserializedJsonAsString = deserializedJsonValue.ToString();
            Assert.Equal(json, deserializedJsonAsString);
        }

        #endregion Body
    }

    public class ActionValueController : ApiController
    {
        // Demonstrates parameter that can come from route, query string, or defaults
        public ActionValueItem Get(int id = 0, string firstName = "DefaultFirstName", string lastName = "DefaultLastName")
        {
            return new ActionValueItem() { Id = id, FirstName = firstName, LastName = lastName };
        }

        // Demonstrates an explicit override to obtain parameters from URL
        public ActionValueItem GetFromUri([FromUri] int id = 0,
                                   [FromUri] string firstName = "DefaultFirstName",
                                   [FromUri] string lastName = "DefaultLastName")
        {
            return new ActionValueItem() { Id = id, FirstName = firstName, LastName = lastName };
        }

        
        // Complex objects default to body. But we can bind from URI with an attribute.
        public ActionValueItem GetItem([FromUri] ActionValueItem item)
        {
            return item;
        }

        // Demonstrates ModelBinding a Item object explicitly from Uri
        public ActionValueItem GetItemFromUri([FromUri] ActionValueItem item)
        {
            return item;
        }

        // Demonstrates use of renaming parameters via prefix
        public ActionValueItem GetFromNamed([FromUri(Prefix = "custID")] int id,
                                     [FromUri(Prefix = "first")] string firstName,
                                     [FromUri(Prefix = "last")] string lastName)
        {
            return new ActionValueItem() { Id = id, FirstName = firstName, LastName = lastName };
        }

        // Demonstrates use of custom ValueProvider via attribute
        public ActionValueItem GetFromCustom([ValueProvider(typeof(ActionValueControllerValueProviderFactory), Prefix = "id")] int id,
                                      [ValueProvider(typeof(ActionValueControllerValueProviderFactory), Prefix = "customFirstName")] string firstName,
                                      [ValueProvider(typeof(ActionValueControllerValueProviderFactory), Prefix = "customLastName")] string lastName)
        {
            return new ActionValueItem() { Id = id, FirstName = firstName, LastName = lastName };
        }

        // Demonstrates ModelBinding to the CancellationToken of the current request
        public string GetFromCancellationToken(CancellationToken cancellationToken)
        {
            return cancellationToken.ToString();
        }

        // Demonstrates ModelBinding to the ModelState of the current request
        public string GetFromModelState(ModelState modelState)
        {
            return modelState.ToString();
        }

        // Demonstrates binding to complex type from body
        public ActionValueItem PostComplexType(ActionValueItem item)
        {
            return item;
        }

        // Demonstrates binding to complex type from uri
        public ActionValueItem PostComplexTypeUri([FromUri] ActionValueItem item)
        {
            return item;
        }

        // Demonstrates binding to IEnumerable of complex type from body or Uri
        public ActionValueItem PostEnumerable(IEnumerable<ActionValueItem> items)
        {
            return items.FirstOrDefault();
        }

        // Demonstrates binding to IEnumerable of complex type from body or Uri
        public ActionValueItem PostEnumerableUri([FromUri] IEnumerable<ActionValueItem> items)
        {
            return items.FirstOrDefault();
        }

        // Demonstrates binding to JsonValue from body
        public JsonValue PostJsonValue(JsonValue jsonValue)
        {
            return jsonValue;
        }

        // Demonstrate what we expect to be the common default scenario. No attributes are required. 
        // A complex object comes from the body, and simple objects come from the URI.
        public ActionValueItem PostFromBodyAndUri(int id, ActionValueItem item)
        {
            return item;
        }

        // Demonstrates binding to complex type explicitly marked as coming from body
        public ActionValueItem PostFromBody([FromBody] ActionValueItem item)
        {
            return item;
        }

        // Demonstrates how body can be shredded to name/value pairs to bind to simple types
        public ActionValueItem PostToSimpleTypes(int id, string firstName, string lastName)
        {
            return new ActionValueItem() { Id = id, FirstName = firstName, LastName = lastName };
        }

        // Demonstrates binding to ObjectContent<T> from request body
        public ActionValueItem PostObjectContentOfItem(ObjectContent<ActionValueItem> item)
        {
            return item.ReadAsAsync<ActionValueItem>().Result;
        }

        public class ActionValueControllerValueProviderFactory : ValueProviderFactory
        {
            public override IValueProvider GetValueProvider(HttpActionContext actionContext)
            {
                return new ActionValueControllerValueProvider();
            }
        }

        public class ActionValueControllerValueProvider : IValueProvider
        {
            public bool ContainsPrefix(string prefix)
            {
                return true;
            }

            public ValueProviderResult GetValue(string key)
            {
                return new ValueProviderResult("99", "99", CultureInfo.CurrentCulture);
            }
        }
    }

    static class DefaultActionValueBinderExtensions
    {
        public static Task BindValuesAsync(this DefaultActionValueBinder binder, HttpActionContext actionContext, CancellationToken cancellationToken)
        {
            HttpActionBinding binding = binder.GetBinding(actionContext.ActionDescriptor);
            return binding.ExecuteBindingAsync(actionContext, cancellationToken);
        }
    }

    public class ActionValueItem
    {
        [Range(0, 99)]
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }
}