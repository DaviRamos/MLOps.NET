﻿using MLOps.NET.Entities.Impl;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MLOps.NET.Storage.Interfaces
{
    /// <summary>
    /// Repository to manage deployment related entities
    /// </summary>
    public interface IDeploymentRepository
    {
        /// <summary>
        /// Creates a deployment target
        /// </summary>
        /// <param name="deploymentTargetName"></param>
        Task CreateDeploymentTargetAsync(string deploymentTargetName);

        /// <summary>
        /// Gets all deployment targets
        /// </summary>
        /// <returns></returns>
        List<DeploymentTarget> GetDeploymentTargets();

        /// <summary>
        /// Creates a deployment
        /// </summary>
        /// <param name="deploymentTarget"></param>
        /// <param name="registeredModel"></param>
        /// <param name="deployedBy"></param>
        Task CreateDeploymentAsync(DeploymentTarget deploymentTarget, RegisteredModel registeredModel, string deployedBy);

        /// <summary>
        /// Gets all deployments by experiment id
        /// </summary>
        /// <param name="experimentId"></param>
        /// <returns></returns>
        List<Deployment> GetDeployments(Guid experimentId);
    }
}