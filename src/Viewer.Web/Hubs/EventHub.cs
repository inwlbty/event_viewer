﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using Viewer.Web.ViewModels.Monitor;

namespace Viewer.Web.Hubs
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class EventHub : Hub<IEventClient>
    {
        public EventHub(IMemoryCache memoryCache)
        {
            MemoryCache = memoryCache;
        }

        public IMemoryCache MemoryCache { get; }

        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            var sAppId = httpContext.Request.Query["application"];
            var appId = long.Parse(sAppId);

            var userId = Context.User.Claims.First(x => x.Type == ClaimTypes.NameIdentifier);
            // 检查当前用户是否和对应的应用程序关联
            // TODO 打算针对每个链接设置获取的事件级别，但通过从服务器发送不方便获取ConnectionId 暂时作罢
            
            var id = Context.ConnectionId;
            MemoryCache.Set(id, new MonitorSettings
            {
                ApplicationId = appId,
                Levels = new HashSet<string>
                {
                    "critical", "error", "warning", "information", "debug", "trace"
                }
            });
            
            // 以应用程序分组
            await Groups.AddToGroupAsync(id, sAppId);
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var httpContext = Context.GetHttpContext();
            var sAppId = httpContext.Request.Query["application"];

            var id = Context.ConnectionId;
            MemoryCache.Remove(id);

            await Groups.RemoveFromGroupAsync(id, sAppId);
        }
    }
}