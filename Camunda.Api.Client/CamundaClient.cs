﻿using System;
using System.Net.Http;
using System.Reflection;
using System.Globalization;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Refit;

using Camunda.Api.Client.CaseDefinition;
using Camunda.Api.Client.CaseExecution;
using Camunda.Api.Client.DecisionDefinition;
using Camunda.Api.Client.Deployment;
using Camunda.Api.Client.Execution;
using Camunda.Api.Client.ExternalTask;
using Camunda.Api.Client.Group;
using Camunda.Api.Client.History;
using Camunda.Api.Client.Incident;
using Camunda.Api.Client.Job;
using Camunda.Api.Client.JobDefinition;
using Camunda.Api.Client.Message;
using Camunda.Api.Client.ProcessDefinition;
using Camunda.Api.Client.ProcessInstance;
using Camunda.Api.Client.Signal;
using Camunda.Api.Client.Tenant;
using Camunda.Api.Client.User;
using Camunda.Api.Client.UserTask;
using Camunda.Api.Client.VariableInstance;

namespace Camunda.Api.Client
{
    public class CamundaClient
    {
        private Lazy<ICaseDefinitionRestService> _caseDefinitionRestService;
        private Lazy<ICaseExecutionRestService> _caseExecutionRestService;
        private Lazy<IDecisionDefinitionRestService> _decisionDefinitionRestService;
        private Lazy<IDeploymentRestService> _deploymentApi;
        private Lazy<IExecutionRestService> _executionApi;
        private Lazy<IExternalTaskRestService> _externalTaskApi;
        private Lazy<IGroupRestService> _groupApi;
        private Lazy<IIncidentRestService> _incidentApi;
        private Lazy<IJobDefinitionRestService> _jobDefinitionApi;
        private Lazy<IJobRestService> _jobApi;
        private Lazy<IMessageRestService> _messageApi;
        private Lazy<IProcessDefinitionRestService> _processDefinitionApi;
        private Lazy<IProcessInstanceRestService> _processInstanceApi;
        private Lazy<ISignalRestService> _signalApi;
        private Lazy<ITenantRestService> _tenantApi;
        private Lazy<IUserRestService> _userApi;
        private Lazy<IUserTaskRestService> _userTaskApi;
        private Lazy<IVariableInstanceRestService> _variableInstanceApi;

        private HistoricApi _historicApi;

        private string _hostUrl;
        private HttpClient _httpClient;

        private RefitSettings _refitSettings;
        private static JsonSerializerSettings _jsonSerializerSettings;
        private HttpMessageHandler _httpMessageHandler;
        internal static JsonSerializerSettings JsonSerializerSettings => _jsonSerializerSettings;

        internal class HistoricApi
        {
            public Lazy<IHistoricActivityInstanceRestService> ActivityInstanceApi;
            public Lazy<IHistoricCaseActivityInstanceRestService> CaseActivityInstanceApi;
            public Lazy<IHistoricCaseDefinitionRestService> CaseDefinitionApi;
            public Lazy<IHistoricCaseInstanceRestService> CaseInstanceApi;
            public Lazy<IHistoricDecisionInstanceRestService> DecisionInstanceApi;
            public Lazy<IHistoricDetailRestService> DetailApi;
            public Lazy<IHistoricIncidentRestService> IncidentApi;
            public Lazy<IHistoricJobLogRestService> JobLogApi;
            public Lazy<IHistoricProcessInstanceRestService> ProcessInstanceApi;
            public Lazy<IHistoricVariableInstanceRestService> VariableInstanceApi;
        }

        static CamundaClient()
        {
            _jsonSerializerSettings = _jsonSerializerSettings ?? new JsonSerializerSettings
            {
                ContractResolver = new CustomCamelCasePropertyNamesContractResolver(),
                NullValueHandling = NullValueHandling.Ignore, // do not send empty fields
            };

            _jsonSerializerSettings.Converters.Add(new StringEnumConverter());
            _jsonSerializerSettings.Converters.Add(new CustomIsoDateTimeConverter());
        }

        private class CustomIsoDateTimeConverter : Newtonsoft.Json.Converters.IsoDateTimeConverter
        {
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                if (value is DateTime dateTime)
                {
                    if ((DateTimeStyles & DateTimeStyles.AdjustToUniversal) == DateTimeStyles.AdjustToUniversal
                        || (DateTimeStyles & DateTimeStyles.AssumeUniversal) == DateTimeStyles.AssumeUniversal)
                    {
                        dateTime = dateTime.ToUniversalTime();
                    }

                    writer.WriteValue(dateTime.ToJavaISO8601());
                }
                else
                {
                    base.WriteJson(writer, value, serializer);
                }
            }
        }

        private void Initialize()
        {
            _httpMessageHandler = _httpMessageHandler ?? new ErrorMessageHandler();

            _refitSettings = _refitSettings ?? new RefitSettings
            {
                JsonSerializerSettings = _jsonSerializerSettings,
                UrlParameterFormatter = new CustomUrlParameterFormatter(),
                HttpMessageHandlerFactory = () => _httpMessageHandler
            };
        }

        private class CustomUrlParameterFormatter : DefaultUrlParameterFormatter
        {
            public override string Format(object parameterValue, ParameterInfo parameterInfo)
            {
                if (parameterValue is bool)
                    return string.Format(CultureInfo.InvariantCulture, "{0}", parameterValue).ToLower();
                else
                    return base.Format(parameterValue, parameterInfo);
            }
        }

        private class CustomCamelCasePropertyNamesContractResolver : CamelCasePropertyNamesContractResolver
        {
            // preserve exact dictionary key
            protected override string ResolveDictionaryKey(string dictionaryKey) => dictionaryKey;
        }

        private CamundaClient(string hostUrl)
        {
            _hostUrl = hostUrl;
            Initialize();
            CreateServices();
        }

        private CamundaClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
            Initialize();
            CreateServices();
        }

        private CamundaClient(string hostUrl, HttpMessageHandler httpMessageHandler)
        {
            _hostUrl = hostUrl;
            _httpMessageHandler = httpMessageHandler;
            Initialize();
            CreateServices();
        }

        private void CreateServices()
        {
            _caseDefinitionRestService = CreateService<ICaseDefinitionRestService>();
            _caseExecutionRestService = CreateService<ICaseExecutionRestService>();
            _decisionDefinitionRestService = CreateService<IDecisionDefinitionRestService>();
            _deploymentApi = CreateService<IDeploymentRestService>();
            _executionApi = CreateService<IExecutionRestService>();
            _externalTaskApi = CreateService<IExternalTaskRestService>();
            _groupApi = CreateService<IGroupRestService>();
            _incidentApi = CreateService<IIncidentRestService>();
            _jobApi = CreateService<IJobRestService>();
            _jobDefinitionApi = CreateService<IJobDefinitionRestService>();
            _messageApi = CreateService<IMessageRestService>();
            _processDefinitionApi = CreateService<IProcessDefinitionRestService>();
            _processInstanceApi = CreateService<IProcessInstanceRestService>();
            _signalApi = CreateService<ISignalRestService>();
            _tenantApi = CreateService<ITenantRestService>();
            _userApi = CreateService<IUserRestService>();
            _userTaskApi = CreateService<IUserTaskRestService>();
            _variableInstanceApi = CreateService<IVariableInstanceRestService>();

            _historicApi = new HistoricApi()
            {
                ActivityInstanceApi = CreateService<IHistoricActivityInstanceRestService>(),
                CaseActivityInstanceApi = CreateService<IHistoricCaseActivityInstanceRestService>(),
                CaseDefinitionApi = CreateService<IHistoricCaseDefinitionRestService>(),
                CaseInstanceApi = CreateService<IHistoricCaseInstanceRestService>(),
                DecisionInstanceApi = CreateService<IHistoricDecisionInstanceRestService>(),
                DetailApi = CreateService<IHistoricDetailRestService>(),
                IncidentApi = CreateService<IHistoricIncidentRestService>(),
                JobLogApi = CreateService<IHistoricJobLogRestService>(),
                ProcessInstanceApi = CreateService<IHistoricProcessInstanceRestService>(),
                VariableInstanceApi = CreateService<IHistoricVariableInstanceRestService>(),
            };
        }

        private Lazy<I> CreateService<I>()
        {
            if (_httpClient != null)
                return new Lazy<I>(() => RestService.For<I>(_httpClient, _refitSettings));
            else
                return new Lazy<I>(() => RestService.For<I>(_hostUrl, _refitSettings));
        }

        public static CamundaClient Create(string hostUrl)
        {
            return new CamundaClient(hostUrl);
        }

        public static CamundaClient Create(string hostUrl, HttpMessageHandler httpMessageHandler)
        {
            return new CamundaClient(hostUrl, httpMessageHandler);
        }

        public static CamundaClient Create(HttpClient httpClient)
        {
            return new CamundaClient(httpClient);
        }

        /// <see href="https://docs.camunda.org/manual/7.9/reference/rest/case-definition/"/>
        public CaseDefinitionService CaseDefinitions => new CaseDefinitionService(_caseDefinitionRestService.Value);

        /// <see href="https://docs.camunda.org/manual/7.9/reference/rest/case-execution/"/>
        public CaseExecutionService CaseExecutions => new CaseExecutionService(_caseExecutionRestService.Value);

        /// <see href="https://docs.camunda.org/manual/7.9/reference/rest/decision-definition/"/>
        public DecisionDefinitionService DecisionDefinitions => new DecisionDefinitionService(_decisionDefinitionRestService.Value);

        /// <see href="https://docs.camunda.org/manual/7.9/reference/rest/deployment/"/>
        public DeploymentService Deployments => new DeploymentService(_deploymentApi.Value);

        /// <see href="https://docs.camunda.org/manual/7.9/reference/rest/execution/"/>
        public ExecutionService Executions => new ExecutionService(_executionApi.Value);

        /// <see href="https://docs.camunda.org/manual/7.9/reference/rest/external-task/"/>
        public ExternalTaskService ExternalTasks => new ExternalTaskService(_externalTaskApi.Value);

        /// <see href="https://docs.camunda.org/manual/7.9/reference/rest/group/"/>
        public GroupService Group => new GroupService(_groupApi.Value);

        /// <see href="https://docs.camunda.org/manual/7.9/reference/rest/history/"/>
        public HistoryService History => new HistoryService(_historicApi);

        /// <see href="https://docs.camunda.org/manual/7.9/reference/rest/incident/"/>
        public IncidentService Incidents => new IncidentService(_incidentApi.Value);

        /// <see href="https://docs.camunda.org/manual/7.9/reference/rest/job-definition/"/>
        public JobDefinitionService JobDefinitions => new JobDefinitionService(_jobDefinitionApi.Value);

        /// <see href="https://docs.camunda.org/manual/7.9/reference/rest/job/"/>
        public JobService Jobs => new JobService(_jobApi.Value);

        /// <see href="https://docs.camunda.org/manual/7.9/reference/rest/message/"/>
        public MessageService Messages => new MessageService(_messageApi.Value);

        /// <see href="https://docs.camunda.org/manual/7.9/reference/rest/process-definition/"/>
        public ProcessDefinitionService ProcessDefinitions => new ProcessDefinitionService(_processDefinitionApi.Value);

        /// <see href="https://docs.camunda.org/manual/7.9/reference/rest/process-instance/"/>
        public ProcessInstanceService ProcessInstances => new ProcessInstanceService(_processInstanceApi.Value);

        /// <see href="https://docs.camunda.org/manual/7.9/reference/rest/signal/"/>
        public SignalService Signals => new SignalService(_signalApi.Value);

        /// <see href="https://docs.camunda.org/manual/7.9/reference/rest/task/"/>
        public TenantService Tenants => new TenantService(_tenantApi.Value);

        /// <see href="https://docs.camunda.org/manual/7.9/reference/rest/tenant/"/>
        public UserService Users => new UserService(_userApi.Value);

        /// <see href="https://docs.camunda.org/manual/7.9/reference/rest/user/"/>
        public UserTaskService UserTasks => new UserTaskService(_userTaskApi.Value);

        /// <see href="https://docs.camunda.org/manual/7.9/reference/rest/variable-instance/"/>
        public VariableInstanceService VariableInstances => new VariableInstanceService(_variableInstanceApi.Value);
    }
}
