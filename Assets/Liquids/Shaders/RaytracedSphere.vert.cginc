//Copyright 2020 Julie#8169 STREAM_DOGS#4199
//
//Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation 
//files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, 
//modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the 
//Software is furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
//OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS 
//BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF
//OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

            float _Radius = length(v.vertex.xyz);
            o.debug = float3(0,0,0);

            half liquidY = _LiquidLevel * _Radius;  
            half scale = length(float3(unity_ObjectToWorld[0].x, unity_ObjectToWorld[1].x, unity_ObjectToWorld[2].x));
         
            // point on the sphere where the ray cast enters
            float4 entryVertex = v.vertex;
            
            // reshape the mesh to get a flat surface with many polygons
            if(_LiquidLevel < 1)
            {
                if(v.vertex.y > liquidY)
                {
                    float3 oldVertex = v.vertex.xyz;
                    float3 oldNormal = v.normal.xyz;
                    float blend = min(1,(v.vertex.y - liquidY)/((1+_LiquidLevel)*0.05/_SurfaceTension));
                    
                    v.vertex.xz = normalize(v.vertex.xz) * sqrt(1-liquidY*liquidY/_Radius/_Radius) * (_Radius-v.vertex.y)/(_Radius-liquidY) * _Radius;                
                    v.vertex.y = liquidY; 

                    // correct the normals for modified vertices
                    float lenxz = length(v.vertex.xz);

                    float distanceToSurfaceCenter = lenxz / _Radius;
                    distanceToSurfaceCenter = pow(distanceToSurfaceCenter, _SurfaceTension); 

                    v.normal = v.vertex.xyz;
                    v.normal.y = 0;
                    v.normal = (distanceToSurfaceCenter * normalize(v.normal) + (1-distanceToSurfaceCenter) * float3(0,1,0) );
 
                    half sine =  sin(10*(lenxz-_Time.x*_WavesSpeed));
                    half w = _WavesIntensity * (sine) * (sine) * (1-distanceToSurfaceCenter); 
                    v.normal = v.normal * (1-w) -  normalize(float3(v.vertex.x, 0, v.vertex.z)) * w; 
                           
                    v.vertex.xyz = blend * v.vertex.xyz + (1-blend) * oldVertex;
                    v.normal.xyz = blend * v.normal.xyz + (1-blend) * oldNormal;
 
                } 
                else if(v.vertex.y > liquidY - 0.05*_Radius)
                {
                    v.vertex.xz = normalize(v.vertex.xz) * sqrt(1-liquidY*liquidY/_Radius/_Radius) * _Radius;                
                    v.vertex.y = liquidY;             
                }
            }
            
            float3 viewdir = -normalize(ObjSpaceViewDir(v.vertex));
            float3 centerProj = v.vertex.xyz + dot(-v.vertex.xyz, viewdir) * viewdir;
            float sqLen = centerProj.x * centerProj.x + centerProj.y * centerProj.y + centerProj.z * centerProj.z;
            float radiusAroundProj = sqrt(max(0,_Radius*_Radius - sqLen));

            float3 sphInter1 = centerProj + viewdir * radiusAroundProj;
            float3 sphInter2 = centerProj - viewdir * radiusAroundProj;
                
            // remove from rayIntersection the proportion of the intersection with the sphere that is under the surface
            float ratio = (liquidY - min(sphInter1.y, sphInter2.y)) / (0.001+abs(sphInter1.y - sphInter2.y));       
            o.rayIntersection = radiusAroundProj * scale * min(1,ratio);
                
    