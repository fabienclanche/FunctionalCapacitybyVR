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

            // compute height and planar distance to center (radius) of the vertex in the target truncated sphere, assuming it has radius of 1
            half y = (v.vertex.y / _Radius + 1) / 2 * min(2, (_LiquidLevel + 1)*(1+_SurfaceTension)) - 1;
            half planarRadius = (y <= _LiquidLevel) ? sqrt(1-y*y) : sqrt(1-y*y)*(1-(y-_LiquidLevel)/_SurfaceTension/2);

            // finds angle between camera-to-vertex ray and vertex-to-center ray
            float3 viewdir = -normalize(ObjSpaceViewDir(v.vertex));
            float3 dir2center = normalize(-v.vertex.xyz);
            float theta = acos(dot(viewdir, dir2center));

            // compute the length of the ray intersection with the sphere
            o.rayIntersection = sqrt(2 + 2 * cos(2 * theta)) * _Radius;
            float3 exitVertex = v.vertex.xyz + viewdir * o.rayIntersection;

            // remove from rayIntersection the proportion of the intersection with the sphere that is under the surface
            float ratio =  ((_LiquidLevel) * _Radius - min(exitVertex.y, v.vertex.y)) / (0.00001+abs(exitVertex.y - v.vertex.y));            
            o.rayIntersection *= min(1,ratio);
 
            if(v.vertex.y > _LiquidLevel * _Radius)
            {
                v.vertex.xyz = exitVertex.xyz - viewdir * o.rayIntersection;
                float distanceToSurfaceCenter = length(v.vertex.xz) / _Radius;
                //  v.vertex.y += (1-distanceToSurfaceCenter)*(_LiquidLevel+1)/100;
                distanceToSurfaceCenter=pow(distanceToSurfaceCenter, _SurfaceTension);
                
                v.normal = v.vertex;
                v.normal.y = 0;
                v.normal = (distanceToSurfaceCenter * normalize(v.normal) + (1-distanceToSurfaceCenter) * float3(0,1,0) );
            }             
 
            o.debug.r = ratio;