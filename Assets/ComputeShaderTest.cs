using UnityEngine;
using UnityEngine.Experimental.Rendering;
using ComputeShaderUtility;
using System.Collections.Generic;
using System.Linq;
public struct Agent
{
    public Vector2 position;
    public float angle;

    public int isAlive;
}

public class ComputeShaderTest : MonoBehaviour
{
    public ComputeShader computeShader;
    public RenderTexture trailMap;
    public RenderTexture diffusedTrailMap;
    public RenderTexture agentTexture;
    public int width = 1000;
    public int height = 1000;
    public static int maxPopulation = 100;
    public int spawnPopulation = 10;
    public static int startingPopulation = 10;

    private int currentPopulation = startingPopulation;
    private int frame = 0;
    ComputeBuffer agentsBuffer;
    Agent[] agents = new Agent[maxPopulation];
    public FilterMode filterMode = FilterMode.Point;
    public GraphicsFormat format = ComputeHelper.defaultGraphicsFormat;
    // Start is called before the first frame update

    public void CreateAgents()
    {
        for (int i = 0; i < maxPopulation; i++)
        {
            float angle = Random.Range(0, 2 * Mathf.PI);
            Agent agentData = new Agent();
            agentData.position = new Vector2(width / 2, height / 2);
            agentData.angle = angle;
            if (i < startingPopulation)
            {
                agentData.isAlive = 1;
            }
            else
            {
                agentData.isAlive = 0;
            }
            agents[i] = agentData;
        }

        ComputeHelper.CreateAndSetBuffer<Agent>(ref agentsBuffer, agents, computeShader, "Agents", kernelIndex: 0);
        computeShader.SetInt("width", width);
        computeShader.SetInt("height", height);
    }

    public void SpawnAgents()
    {
        if (currentPopulation < maxPopulation)
        {
            Debug.Log(currentPopulation);
            agentsBuffer.GetData(agents);
            int preSpawnPop = currentPopulation;
            for (int i = preSpawnPop; i < maxPopulation; i++)
            {
                if ((currentPopulation - preSpawnPop) + 1 <= spawnPopulation)
                {
                    agents[i].isAlive = 1;
                    currentPopulation += 1;
                }
                else
                {
                    break;
                }
            }

            agentsBuffer.SetData(agents);
        }
        else if (currentPopulation > maxPopulation)
        {
            throw new System.Exception("Population has exceeded maximum size.");
        }
    }

    public void GPUCompute()
    {
        ComputeHelper.Dispatch(computeShader, maxPopulation, 1, 1, kernelIndex: 0);
        ComputeHelper.Dispatch(computeShader, width, height, 1, kernelIndex: 1);
    }

    protected virtual void Start()
    {
        ComputeHelper.CreateRenderTexture(ref trailMap, width, height, filterMode, format);
        ComputeHelper.CreateRenderTexture(ref diffusedTrailMap, width, height, filterMode, format);
        ComputeHelper.CreateRenderTexture(ref agentTexture, width, height, filterMode, format);

        computeShader.SetTexture(0, "TrailMap", trailMap);
        computeShader.SetTexture(1, "TrailMap", trailMap);
        computeShader.SetTexture(1, "DiffusedTrailMap", diffusedTrailMap);
        computeShader.SetTexture(1, "AgentsTexture", agentTexture);
        CreateAgents();

        transform.GetComponentInChildren<MeshRenderer>().material.mainTexture = diffusedTrailMap;
    }

    void FixedUpdate()
    {
        if (frame % 149 == 0)
        {
            SpawnAgents();
        }
        for (int i = 0; i < 1; i++)
        {
            GPUCompute();
        }
        frame += 1;

    }

    void OnDestroy()
    {
        ComputeHelper.Release(agentsBuffer);
    }
}