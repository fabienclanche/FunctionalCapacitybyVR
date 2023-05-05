module handle()
{
    minkowski()
    {
        difference()
        {
            hull()
            {
                translate([25,0,-8])
                    rotate([90,30,0]) scale([1,1.5,1]) cylinder(h=0.1, r=12);
                translate([36,0,-3])
                    rotate([90,0,0]) scale([1,1,1]) cylinder(h=0.1, r=12);
            }
            
            hull()
            {
                translate([25,0,-8])
                    rotate([90,30,0]) scale([1,1.5,1]) cylinder(h=0.1, r=11);
                translate([36,0,-3])
                    rotate([90,0,0]) scale([1,1,1]) cylinder(h=0.1, r=11);
            }
            
            sphere(r = 31.75);
            hollowing();
        }
        scale([1,3.3,1]) sphere(1.75);
    }
}

module hollowing()
{
    sphere(r = 30);
    translate([-200,-200,10]) cube([400,400,100]);
        
    translate([0,0,-33]) scale([0.9,0.9,0.08])sphere(r = 20, $fn = 18);
    translate([0,0,-63]) cylinder(r = 20, h = 30);
}

module deco_hollowing()
{
    for(i = [0:1:8]) rotate([0,0,40*i]) translate([34,0,0]) rotate([0,18,0])
    {
        scale([2.5,12,55]) sphere(r=1, $fn=4);
    }
}

module cup()
{
    difference()
    {
        union()
        { 
            scale([1,1,1.15]) sphere(r = 32);
            translate([0,0,-33]) cylinder(r = 20, h = 30); 
        }
        
        hollowing();
        //deco_hollowing();
    }
    
    translate([0,0,-31]) cylinder(r = 20, h = 2); 
    translate([0,0,-33]) ;
}

module color1()
{
    intersection()
    {
        cup();
        union()
        {
            difference()
            {
                translate([0,0,-32.5]) cylinder(r = 20, h = 5); 
                translate([0,0,-33]) cylinder(r = 18, h = 11); 
            }
            translate([0,0,6]) cylinder(r = 50, h = 50); 
        }
    }
    handle();
}
 
module color2()
{
    intersection()
    {
        cup();
        
        translate([0,0,-50]) cylinder(r = 50, h = 56.5); 
        union()
        {
            scale([1,1,1.15]) sphere(r = 31.75);
            translate([0,0,-33]) cylinder(r = 19.75, h = 30); 
        }
    } 
}
 
color([1,1,1]) color2();  
//color1();