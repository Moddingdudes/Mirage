"use strict";(self.webpackChunkmirage_docs=self.webpackChunkmirage_docs||[]).push([[578],{3905:(e,t,a)=>{a.d(t,{Zo:()=>s,kt:()=>k});var l=a(67294);function n(e,t,a){return t in e?Object.defineProperty(e,t,{value:a,enumerable:!0,configurable:!0,writable:!0}):e[t]=a,e}function r(e,t){var a=Object.keys(e);if(Object.getOwnPropertySymbols){var l=Object.getOwnPropertySymbols(e);t&&(l=l.filter((function(t){return Object.getOwnPropertyDescriptor(e,t).enumerable}))),a.push.apply(a,l)}return a}function i(e){for(var t=1;t<arguments.length;t++){var a=null!=arguments[t]?arguments[t]:{};t%2?r(Object(a),!0).forEach((function(t){n(e,t,a[t])})):Object.getOwnPropertyDescriptors?Object.defineProperties(e,Object.getOwnPropertyDescriptors(a)):r(Object(a)).forEach((function(t){Object.defineProperty(e,t,Object.getOwnPropertyDescriptor(a,t))}))}return e}function o(e,t){if(null==e)return{};var a,l,n=function(e,t){if(null==e)return{};var a,l,n={},r=Object.keys(e);for(l=0;l<r.length;l++)a=r[l],t.indexOf(a)>=0||(n[a]=e[a]);return n}(e,t);if(Object.getOwnPropertySymbols){var r=Object.getOwnPropertySymbols(e);for(l=0;l<r.length;l++)a=r[l],t.indexOf(a)>=0||Object.prototype.propertyIsEnumerable.call(e,a)&&(n[a]=e[a])}return n}var c=l.createContext({}),d=function(e){var t=l.useContext(c),a=t;return e&&(a="function"==typeof e?e(t):i(i({},t),e)),a},s=function(e){var t=d(e.components);return l.createElement(c.Provider,{value:t},e.children)},p={inlineCode:"code",wrapper:function(e){var t=e.children;return l.createElement(l.Fragment,{},t)}},u=l.forwardRef((function(e,t){var a=e.components,n=e.mdxType,r=e.originalType,c=e.parentName,s=o(e,["components","mdxType","originalType","parentName"]),u=d(a),k=n,m=u["".concat(c,".").concat(k)]||u[k]||p[k]||r;return a?l.createElement(m,i(i({ref:t},s),{},{components:a})):l.createElement(m,i({ref:t},s))}));function k(e,t){var a=arguments,n=t&&t.mdxType;if("string"==typeof e||n){var r=a.length,i=new Array(r);i[0]=u;var o={};for(var c in t)hasOwnProperty.call(t,c)&&(o[c]=t[c]);o.originalType=e,o.mdxType="string"==typeof e?e:n,i[1]=o;for(var d=2;d<r;d++)i[d]=a[d];return l.createElement.apply(null,i)}return l.createElement.apply(null,a)}u.displayName="MDXCreateElement"},69554:(e,t,a)=>{a.r(t),a.d(t,{assets:()=>c,contentTitle:()=>i,default:()=>p,frontMatter:()=>r,metadata:()=>o,toc:()=>d});var l=a(87462),n=(a(67294),a(3905));const r={id:"NetworkServer",title:"NetworkServer"},i="Class NetworkServer",o={unversionedId:"reference/Mirage/NetworkServer",id:"reference/Mirage/NetworkServer",title:"NetworkServer",description:"The NetworkServer.",source:"@site/docs/reference/Mirage/NetworkServer.md",sourceDirName:"reference/Mirage",slug:"/reference/Mirage/NetworkServer",permalink:"/Mirage/docs/reference/Mirage/NetworkServer",draft:!1,editUrl:"https://github.com/MirageNet/Mirage/tree/master/doc/docs/reference/Mirage/NetworkServer.md",tags:[],version:"current",frontMatter:{id:"NetworkServer",title:"NetworkServer"},sidebar:"api",previous:{title:"NetworkSceneManager",permalink:"/Mirage/docs/reference/Mirage/NetworkSceneManager"},next:{title:"NetworkTime",permalink:"/Mirage/docs/reference/Mirage/NetworkTime"}},c={},d=[{value:"Inheritance",id:"inheritance",level:5},{value:"Syntax",id:"syntax",level:5},{value:"Fields",id:"fields",level:3},{value:"EnablePeerMetrics",id:"enablepeermetrics",level:4},{value:"Declaration",id:"declaration",level:5},{value:"MetricsSize",id:"metricssize",level:4},{value:"Declaration",id:"declaration-1",level:5},{value:"MaxConnections",id:"maxconnections",level:4},{value:"Declaration",id:"declaration-2",level:5},{value:"DisconnectOnException",id:"disconnectonexception",level:4},{value:"Declaration",id:"declaration-3",level:5},{value:"RunInBackground",id:"runinbackground",level:4},{value:"Declaration",id:"declaration-4",level:5},{value:"Listening",id:"listening",level:4},{value:"Declaration",id:"declaration-5",level:5},{value:"SocketFactory",id:"socketfactory",level:4},{value:"Declaration",id:"declaration-6",level:5},{value:"authenticator",id:"authenticator",level:4},{value:"Declaration",id:"declaration-7",level:5},{value:"Properties",id:"properties",level:3},{value:"Metrics",id:"metrics",level:4},{value:"Declaration",id:"declaration-8",level:5},{value:"PeerConfig",id:"peerconfig",level:4},{value:"Declaration",id:"declaration-9",level:5},{value:"Started",id:"started",level:4},{value:"Declaration",id:"declaration-10",level:5},{value:"Connected",id:"connected",level:4},{value:"Declaration",id:"declaration-11",level:5},{value:"Authenticated",id:"authenticated",level:4},{value:"Declaration",id:"declaration-12",level:5},{value:"Disconnected",id:"disconnected",level:4},{value:"Declaration",id:"declaration-13",level:5},{value:"Stopped",id:"stopped",level:4},{value:"Declaration",id:"declaration-14",level:5},{value:"OnStartHost",id:"onstarthost",level:4},{value:"Declaration",id:"declaration-15",level:5},{value:"OnStopHost",id:"onstophost",level:4},{value:"Declaration",id:"declaration-16",level:5},{value:"LocalPlayer",id:"localplayer",level:4},{value:"Declaration",id:"declaration-17",level:5},{value:"LocalClient",id:"localclient",level:4},{value:"Declaration",id:"declaration-18",level:5},{value:"LocalClientActive",id:"localclientactive",level:4},{value:"Declaration",id:"declaration-19",level:5},{value:"NumberOfPlayers",id:"numberofplayers",level:4},{value:"Declaration",id:"declaration-20",level:5},{value:"Players",id:"players",level:4},{value:"Declaration",id:"declaration-21",level:5},{value:"Active",id:"active",level:4},{value:"Declaration",id:"declaration-22",level:5},{value:"World",id:"world",level:4},{value:"Declaration",id:"declaration-23",level:5},{value:"SyncVarSender",id:"syncvarsender",level:4},{value:"Declaration",id:"declaration-24",level:5},{value:"MessageHandler",id:"messagehandler",level:4},{value:"Declaration",id:"declaration-25",level:5},{value:"Methods",id:"methods",level:3},{value:"Stop()",id:"stop",level:4},{value:"Declaration",id:"declaration-26",level:5},{value:"StartServer(NetworkClient)",id:"startservernetworkclient",level:4},{value:"Declaration",id:"declaration-27",level:5},{value:"Parameters",id:"parameters",level:5},{value:"AddConnection(INetworkPlayer)",id:"addconnectioninetworkplayer",level:4},{value:"Declaration",id:"declaration-28",level:5},{value:"Parameters",id:"parameters-1",level:5},{value:"RemoveConnection(INetworkPlayer)",id:"removeconnectioninetworkplayer",level:4},{value:"Declaration",id:"declaration-29",level:5},{value:"Parameters",id:"parameters-2",level:5},{value:"SendToAll&lt;T&gt;(T, Int32)",id:"sendtoalltt-int32",level:4},{value:"Declaration",id:"declaration-30",level:5},{value:"Parameters",id:"parameters-3",level:5},{value:"SendToMany&lt;T&gt;(IEnumerable&lt;INetworkPlayer&gt;, T, Int32)",id:"sendtomanytienumerableinetworkplayer-t-int32",level:4},{value:"Declaration",id:"declaration-31",level:5},{value:"Parameters",id:"parameters-4",level:5},{value:"SendToMany&lt;T&gt;(IReadOnlyList&lt;INetworkPlayer&gt;, T, Int32)",id:"sendtomanytireadonlylistinetworkplayer-t-int32",level:4},{value:"Declaration",id:"declaration-32",level:5},{value:"Parameters",id:"parameters-5",level:5},{value:"SendToManyExcept&lt;T&gt;(IEnumerable&lt;INetworkPlayer&gt;, INetworkPlayer, T, Int32)",id:"sendtomanyexcepttienumerableinetworkplayer-inetworkplayer-t-int32",level:4},{value:"Declaration",id:"declaration-33",level:5},{value:"Parameters",id:"parameters-6",level:5}],s={toc:d};function p(e){let{components:t,...a}=e;return(0,n.kt)("wrapper",(0,l.Z)({},s,a,{components:t,mdxType:"MDXLayout"}),(0,n.kt)("h1",{id:"class-networkserver"},"Class NetworkServer"),(0,n.kt)("p",null,"The NetworkServer."),(0,n.kt)("div",{class:"inheritance"},(0,n.kt)("h5",{id:"inheritance"},"Inheritance"),(0,n.kt)("div",{class:"level",style:{"--data-index":0}},"System.Object")),(0,n.kt)("h5",{id:"syntax"},"Syntax"),(0,n.kt)("pre",null,(0,n.kt)("code",{parentName:"pre",className:"language-cs"},"public class NetworkServer : MonoBehaviour\n")),(0,n.kt)("h3",{id:"fields"},"Fields"),(0,n.kt)("h4",{id:"enablepeermetrics"},"EnablePeerMetrics"),(0,n.kt)("h5",{id:"declaration"},"Declaration"),(0,n.kt)("pre",null,(0,n.kt)("code",{parentName:"pre",className:"language-cs"},"public bool EnablePeerMetrics\n")),(0,n.kt)("h4",{id:"metricssize"},"MetricsSize"),(0,n.kt)("h5",{id:"declaration-1"},"Declaration"),(0,n.kt)("pre",null,(0,n.kt)("code",{parentName:"pre",className:"language-cs"},"public int MetricsSize\n")),(0,n.kt)("h4",{id:"maxconnections"},"MaxConnections"),(0,n.kt)("p",null,"The maximum number of concurrent network connections to support. Excluding the host player.\nThis field is only used if the  property is null"),(0,n.kt)("h5",{id:"declaration-2"},"Declaration"),(0,n.kt)("pre",null,(0,n.kt)("code",{parentName:"pre",className:"language-cs"},"public int MaxConnections\n")),(0,n.kt)("h4",{id:"disconnectonexception"},"DisconnectOnException"),(0,n.kt)("h5",{id:"declaration-3"},"Declaration"),(0,n.kt)("pre",null,(0,n.kt)("code",{parentName:"pre",className:"language-cs"},"public bool DisconnectOnException\n")),(0,n.kt)("h4",{id:"runinbackground"},"RunInBackground"),(0,n.kt)("h5",{id:"declaration-4"},"Declaration"),(0,n.kt)("pre",null,(0,n.kt)("code",{parentName:"pre",className:"language-cs"},"public bool RunInBackground\n")),(0,n.kt)("h4",{id:"listening"},"Listening"),(0,n.kt)("h5",{id:"declaration-5"},"Declaration"),(0,n.kt)("pre",null,(0,n.kt)("code",{parentName:"pre",className:"language-cs"},"public bool Listening\n")),(0,n.kt)("h4",{id:"socketfactory"},"SocketFactory"),(0,n.kt)("h5",{id:"declaration-6"},"Declaration"),(0,n.kt)("pre",null,(0,n.kt)("code",{parentName:"pre",className:"language-cs"},"public SocketFactory SocketFactory\n")),(0,n.kt)("h4",{id:"authenticator"},"authenticator"),(0,n.kt)("h5",{id:"declaration-7"},"Declaration"),(0,n.kt)("pre",null,(0,n.kt)("code",{parentName:"pre",className:"language-cs"},"public NetworkAuthenticator authenticator\n")),(0,n.kt)("h3",{id:"properties"},"Properties"),(0,n.kt)("h4",{id:"metrics"},"Metrics"),(0,n.kt)("h5",{id:"declaration-8"},"Declaration"),(0,n.kt)("pre",null,(0,n.kt)("code",{parentName:"pre",className:"language-cs"},"public Metrics Metrics { get; }\n")),(0,n.kt)("h4",{id:"peerconfig"},"PeerConfig"),(0,n.kt)("p",null,"Config for peer, if not set will use default settings"),(0,n.kt)("h5",{id:"declaration-9"},"Declaration"),(0,n.kt)("pre",null,(0,n.kt)("code",{parentName:"pre",className:"language-cs"},"public Config PeerConfig { get; set; }\n")),(0,n.kt)("h4",{id:"started"},"Started"),(0,n.kt)("p",null,"This is invoked when a server is started - including when a host is started."),(0,n.kt)("h5",{id:"declaration-10"},"Declaration"),(0,n.kt)("pre",null,(0,n.kt)("code",{parentName:"pre",className:"language-cs"},"public IAddLateEvent Started { get; }\n")),(0,n.kt)("h4",{id:"connected"},"Connected"),(0,n.kt)("h5",{id:"declaration-11"},"Declaration"),(0,n.kt)("pre",null,(0,n.kt)("code",{parentName:"pre",className:"language-cs"},"public NetworkPlayerEvent Connected { get; }\n")),(0,n.kt)("h4",{id:"authenticated"},"Authenticated"),(0,n.kt)("h5",{id:"declaration-12"},"Declaration"),(0,n.kt)("pre",null,(0,n.kt)("code",{parentName:"pre",className:"language-cs"},"public NetworkPlayerEvent Authenticated { get; }\n")),(0,n.kt)("h4",{id:"disconnected"},"Disconnected"),(0,n.kt)("h5",{id:"declaration-13"},"Declaration"),(0,n.kt)("pre",null,(0,n.kt)("code",{parentName:"pre",className:"language-cs"},"public NetworkPlayerEvent Disconnected { get; }\n")),(0,n.kt)("h4",{id:"stopped"},"Stopped"),(0,n.kt)("h5",{id:"declaration-14"},"Declaration"),(0,n.kt)("pre",null,(0,n.kt)("code",{parentName:"pre",className:"language-cs"},"public IAddLateEvent Stopped { get; }\n")),(0,n.kt)("h4",{id:"onstarthost"},"OnStartHost"),(0,n.kt)("h5",{id:"declaration-15"},"Declaration"),(0,n.kt)("pre",null,(0,n.kt)("code",{parentName:"pre",className:"language-cs"},"public IAddLateEvent OnStartHost { get; }\n")),(0,n.kt)("h4",{id:"onstophost"},"OnStopHost"),(0,n.kt)("h5",{id:"declaration-16"},"Declaration"),(0,n.kt)("pre",null,(0,n.kt)("code",{parentName:"pre",className:"language-cs"},"public IAddLateEvent OnStopHost { get; }\n")),(0,n.kt)("h4",{id:"localplayer"},"LocalPlayer"),(0,n.kt)("p",null,"The connection to the host mode client (if any)."),(0,n.kt)("h5",{id:"declaration-17"},"Declaration"),(0,n.kt)("pre",null,(0,n.kt)("code",{parentName:"pre",className:"language-cs"},"public INetworkPlayer LocalPlayer { get; }\n")),(0,n.kt)("h4",{id:"localclient"},"LocalClient"),(0,n.kt)("p",null,"The host client for this server "),(0,n.kt)("h5",{id:"declaration-18"},"Declaration"),(0,n.kt)("pre",null,(0,n.kt)("code",{parentName:"pre",className:"language-cs"},"public NetworkClient LocalClient { get; }\n")),(0,n.kt)("h4",{id:"localclientactive"},"LocalClientActive"),(0,n.kt)("p",null,"True if there is a local client connected to this server (host mode)"),(0,n.kt)("h5",{id:"declaration-19"},"Declaration"),(0,n.kt)("pre",null,(0,n.kt)("code",{parentName:"pre",className:"language-cs"},"public bool LocalClientActive { get; }\n")),(0,n.kt)("h4",{id:"numberofplayers"},"NumberOfPlayers"),(0,n.kt)("p",null,"Number of active player objects across all connections on the server.\nThis is only valid on the host / server."),(0,n.kt)("h5",{id:"declaration-20"},"Declaration"),(0,n.kt)("pre",null,(0,n.kt)("code",{parentName:"pre",className:"language-cs"},"public int NumberOfPlayers { get; }\n")),(0,n.kt)("h4",{id:"players"},"Players"),(0,n.kt)("p",null,"A list of local connections on the server."),(0,n.kt)("h5",{id:"declaration-21"},"Declaration"),(0,n.kt)("pre",null,(0,n.kt)("code",{parentName:"pre",className:"language-cs"},"public IReadOnlyCollection<INetworkPlayer> Players { get; }\n")),(0,n.kt)("h4",{id:"active"},"Active"),(0,n.kt)("p",null,"Checks if the server has been started.\nThis will be true after NetworkServer.Listen() has been called."),(0,n.kt)("h5",{id:"declaration-22"},"Declaration"),(0,n.kt)("pre",null,(0,n.kt)("code",{parentName:"pre",className:"language-cs"},"public bool Active { get; }\n")),(0,n.kt)("h4",{id:"world"},"World"),(0,n.kt)("h5",{id:"declaration-23"},"Declaration"),(0,n.kt)("pre",null,(0,n.kt)("code",{parentName:"pre",className:"language-cs"},"public NetworkWorld World { get; }\n")),(0,n.kt)("h4",{id:"syncvarsender"},"SyncVarSender"),(0,n.kt)("h5",{id:"declaration-24"},"Declaration"),(0,n.kt)("pre",null,(0,n.kt)("code",{parentName:"pre",className:"language-cs"},"public SyncVarSender SyncVarSender { get; }\n")),(0,n.kt)("h4",{id:"messagehandler"},"MessageHandler"),(0,n.kt)("h5",{id:"declaration-25"},"Declaration"),(0,n.kt)("pre",null,(0,n.kt)("code",{parentName:"pre",className:"language-cs"},"public MessageHandler MessageHandler { get; }\n")),(0,n.kt)("h3",{id:"methods"},"Methods"),(0,n.kt)("h4",{id:"stop"},"Stop()"),(0,n.kt)("p",null,"This shuts down the server and disconnects all clients.\nIf In host mode, this will also stop the local client"),(0,n.kt)("h5",{id:"declaration-26"},"Declaration"),(0,n.kt)("pre",null,(0,n.kt)("code",{parentName:"pre",className:"language-cs"},"public void Stop()\n")),(0,n.kt)("h4",{id:"startservernetworkclient"},"StartServer(NetworkClient)"),(0,n.kt)("p",null,"Start the server\nIf localClient is given then will start in host mode"),(0,n.kt)("h5",{id:"declaration-27"},"Declaration"),(0,n.kt)("pre",null,(0,n.kt)("code",{parentName:"pre",className:"language-cs"},"public void StartServer(NetworkClient localClient = null)\n")),(0,n.kt)("h5",{id:"parameters"},"Parameters"),(0,n.kt)("table",null,(0,n.kt)("thead",{parentName:"table"},(0,n.kt)("tr",{parentName:"thead"},(0,n.kt)("th",{parentName:"tr",align:null},"Type"),(0,n.kt)("th",{parentName:"tr",align:null},"Name"),(0,n.kt)("th",{parentName:"tr",align:null},"Description"))),(0,n.kt)("tbody",{parentName:"table"},(0,n.kt)("tr",{parentName:"tbody"},(0,n.kt)("td",{parentName:"tr",align:null},"Mirage.NetworkClient"),(0,n.kt)("td",{parentName:"tr",align:null},"localClient"),(0,n.kt)("td",{parentName:"tr",align:null},"if not null then start the server and client in hostmode")))),(0,n.kt)("h4",{id:"addconnectioninetworkplayer"},"AddConnection(INetworkPlayer)"),(0,n.kt)("p",null,"This accepts a network connection and adds it to the server.\nThis connection will use the callbacks registered with the server."),(0,n.kt)("h5",{id:"declaration-28"},"Declaration"),(0,n.kt)("pre",null,(0,n.kt)("code",{parentName:"pre",className:"language-cs"},"public void AddConnection(INetworkPlayer player)\n")),(0,n.kt)("h5",{id:"parameters-1"},"Parameters"),(0,n.kt)("table",null,(0,n.kt)("thead",{parentName:"table"},(0,n.kt)("tr",{parentName:"thead"},(0,n.kt)("th",{parentName:"tr",align:null},"Type"),(0,n.kt)("th",{parentName:"tr",align:null},"Name"),(0,n.kt)("th",{parentName:"tr",align:null},"Description"))),(0,n.kt)("tbody",{parentName:"table"},(0,n.kt)("tr",{parentName:"tbody"},(0,n.kt)("td",{parentName:"tr",align:null},"Mirage.INetworkPlayer"),(0,n.kt)("td",{parentName:"tr",align:null},"player"),(0,n.kt)("td",{parentName:"tr",align:null},"Network connection to add.")))),(0,n.kt)("h4",{id:"removeconnectioninetworkplayer"},"RemoveConnection(INetworkPlayer)"),(0,n.kt)("p",null,"This removes an external connection."),(0,n.kt)("h5",{id:"declaration-29"},"Declaration"),(0,n.kt)("pre",null,(0,n.kt)("code",{parentName:"pre",className:"language-cs"},"public void RemoveConnection(INetworkPlayer player)\n")),(0,n.kt)("h5",{id:"parameters-2"},"Parameters"),(0,n.kt)("table",null,(0,n.kt)("thead",{parentName:"table"},(0,n.kt)("tr",{parentName:"thead"},(0,n.kt)("th",{parentName:"tr",align:null},"Type"),(0,n.kt)("th",{parentName:"tr",align:null},"Name"),(0,n.kt)("th",{parentName:"tr",align:null},"Description"))),(0,n.kt)("tbody",{parentName:"table"},(0,n.kt)("tr",{parentName:"tbody"},(0,n.kt)("td",{parentName:"tr",align:null},"Mirage.INetworkPlayer"),(0,n.kt)("td",{parentName:"tr",align:null},"player"),(0,n.kt)("td",{parentName:"tr",align:null})))),(0,n.kt)("h4",{id:"sendtoalltt-int32"},"SendToAll","<","T",">","(T, Int32)"),(0,n.kt)("p",null,"Send a message to all connected clients."),(0,n.kt)("h5",{id:"declaration-30"},"Declaration"),(0,n.kt)("pre",null,(0,n.kt)("code",{parentName:"pre",className:"language-cs"},"public void SendToAll<T>(T msg, int channelId = 0)\n")),(0,n.kt)("h5",{id:"parameters-3"},"Parameters"),(0,n.kt)("table",null,(0,n.kt)("thead",{parentName:"table"},(0,n.kt)("tr",{parentName:"thead"},(0,n.kt)("th",{parentName:"tr",align:null},"Type"),(0,n.kt)("th",{parentName:"tr",align:null},"Name"),(0,n.kt)("th",{parentName:"tr",align:null},"Description"))),(0,n.kt)("tbody",{parentName:"table"},(0,n.kt)("tr",{parentName:"tbody"},(0,n.kt)("td",{parentName:"tr",align:null},"T"),(0,n.kt)("td",{parentName:"tr",align:null},"msg"),(0,n.kt)("td",{parentName:"tr",align:null},"Message")),(0,n.kt)("tr",{parentName:"tbody"},(0,n.kt)("td",{parentName:"tr",align:null},"System.Int32"),(0,n.kt)("td",{parentName:"tr",align:null},"channelId"),(0,n.kt)("td",{parentName:"tr",align:null},"Transport channel to use")))),(0,n.kt)("h4",{id:"sendtomanytienumerableinetworkplayer-t-int32"},"SendToMany","<","T",">","(IEnumerable","<","INetworkPlayer",">",", T, Int32)"),(0,n.kt)("p",null,"Sends a message to many connections\nWARNING: using this method may cause Enumerator to be boxed creating GC/alloc. Use  version where possible"),(0,n.kt)("h5",{id:"declaration-31"},"Declaration"),(0,n.kt)("pre",null,(0,n.kt)("code",{parentName:"pre",className:"language-cs"},"public static void SendToMany<T>(IEnumerable<INetworkPlayer> players, T msg, int channelId = 0)\n")),(0,n.kt)("h5",{id:"parameters-4"},"Parameters"),(0,n.kt)("table",null,(0,n.kt)("thead",{parentName:"table"},(0,n.kt)("tr",{parentName:"thead"},(0,n.kt)("th",{parentName:"tr",align:null},"Type"),(0,n.kt)("th",{parentName:"tr",align:null},"Name"),(0,n.kt)("th",{parentName:"tr",align:null},"Description"))),(0,n.kt)("tbody",{parentName:"table"},(0,n.kt)("tr",{parentName:"tbody"},(0,n.kt)("td",{parentName:"tr",align:null},"System.Collections.Generic.IEnumerable","<","Mirage.INetworkPlayer",">"),(0,n.kt)("td",{parentName:"tr",align:null},"players"),(0,n.kt)("td",{parentName:"tr",align:null})),(0,n.kt)("tr",{parentName:"tbody"},(0,n.kt)("td",{parentName:"tr",align:null},"T"),(0,n.kt)("td",{parentName:"tr",align:null},"msg"),(0,n.kt)("td",{parentName:"tr",align:null})),(0,n.kt)("tr",{parentName:"tbody"},(0,n.kt)("td",{parentName:"tr",align:null},"System.Int32"),(0,n.kt)("td",{parentName:"tr",align:null},"channelId"),(0,n.kt)("td",{parentName:"tr",align:null})))),(0,n.kt)("h4",{id:"sendtomanytireadonlylistinetworkplayer-t-int32"},"SendToMany","<","T",">","(IReadOnlyList","<","INetworkPlayer",">",", T, Int32)"),(0,n.kt)("p",null,"Sends a message to many connections"),(0,n.kt)("p",null,"Same as  but uses for loop to avoid allocations"),(0,n.kt)("h5",{id:"declaration-32"},"Declaration"),(0,n.kt)("pre",null,(0,n.kt)("code",{parentName:"pre",className:"language-cs"},"public static void SendToMany<T>(IReadOnlyList<INetworkPlayer> players, T msg, int channelId = 0)\n")),(0,n.kt)("h5",{id:"parameters-5"},"Parameters"),(0,n.kt)("table",null,(0,n.kt)("thead",{parentName:"table"},(0,n.kt)("tr",{parentName:"thead"},(0,n.kt)("th",{parentName:"tr",align:null},"Type"),(0,n.kt)("th",{parentName:"tr",align:null},"Name"),(0,n.kt)("th",{parentName:"tr",align:null},"Description"))),(0,n.kt)("tbody",{parentName:"table"},(0,n.kt)("tr",{parentName:"tbody"},(0,n.kt)("td",{parentName:"tr",align:null},"System.Collections.Generic.IReadOnlyList","<","Mirage.INetworkPlayer",">"),(0,n.kt)("td",{parentName:"tr",align:null},"players"),(0,n.kt)("td",{parentName:"tr",align:null})),(0,n.kt)("tr",{parentName:"tbody"},(0,n.kt)("td",{parentName:"tr",align:null},"T"),(0,n.kt)("td",{parentName:"tr",align:null},"msg"),(0,n.kt)("td",{parentName:"tr",align:null})),(0,n.kt)("tr",{parentName:"tbody"},(0,n.kt)("td",{parentName:"tr",align:null},"System.Int32"),(0,n.kt)("td",{parentName:"tr",align:null},"channelId"),(0,n.kt)("td",{parentName:"tr",align:null})))),(0,n.kt)("h4",{id:"sendtomanyexcepttienumerableinetworkplayer-inetworkplayer-t-int32"},"SendToManyExcept","<","T",">","(IEnumerable","<","INetworkPlayer",">",", INetworkPlayer, T, Int32)"),(0,n.kt)("p",null,"Sends a message to many connections, expect excluded."),(0,n.kt)("p",null,"This can be useful if you want to send to a observers of an object expect the owner. Or if you want to send to all expect the local host player."),(0,n.kt)("p",null,"WARNING: using this method may cause Enumerator to be boxed creating GC/alloc. Use  version where possible"),(0,n.kt)("h5",{id:"declaration-33"},"Declaration"),(0,n.kt)("pre",null,(0,n.kt)("code",{parentName:"pre",className:"language-cs"},"public static void SendToManyExcept<T>(IEnumerable<INetworkPlayer> players, INetworkPlayer excluded, T msg, int channelId = 0)\n")),(0,n.kt)("h5",{id:"parameters-6"},"Parameters"),(0,n.kt)("table",null,(0,n.kt)("thead",{parentName:"table"},(0,n.kt)("tr",{parentName:"thead"},(0,n.kt)("th",{parentName:"tr",align:null},"Type"),(0,n.kt)("th",{parentName:"tr",align:null},"Name"),(0,n.kt)("th",{parentName:"tr",align:null},"Description"))),(0,n.kt)("tbody",{parentName:"table"},(0,n.kt)("tr",{parentName:"tbody"},(0,n.kt)("td",{parentName:"tr",align:null},"System.Collections.Generic.IEnumerable","<","Mirage.INetworkPlayer",">"),(0,n.kt)("td",{parentName:"tr",align:null},"players"),(0,n.kt)("td",{parentName:"tr",align:null})),(0,n.kt)("tr",{parentName:"tbody"},(0,n.kt)("td",{parentName:"tr",align:null},"Mirage.INetworkPlayer"),(0,n.kt)("td",{parentName:"tr",align:null},"excluded"),(0,n.kt)("td",{parentName:"tr",align:null},"player to exclude, Can be null")),(0,n.kt)("tr",{parentName:"tbody"},(0,n.kt)("td",{parentName:"tr",align:null},"T"),(0,n.kt)("td",{parentName:"tr",align:null},"msg"),(0,n.kt)("td",{parentName:"tr",align:null})),(0,n.kt)("tr",{parentName:"tbody"},(0,n.kt)("td",{parentName:"tr",align:null},"System.Int32"),(0,n.kt)("td",{parentName:"tr",align:null},"channelId"),(0,n.kt)("td",{parentName:"tr",align:null})))))}p.isMDXComponent=!0}}]);