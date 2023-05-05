
module base()
{
    hull()
    {
        cylinder(r=28, h=10, center=true);  
        translate([+8,35,0]) cylinder(r=25, h=10, center=true); 
    }
    
    hull()
    {
        translate([+8,35,0]) cylinder(r=25, h=10, center=true); 
        translate([+10,75,0]) cylinder(r=27, h=10, center=true); 
    }

    hull()
    { 
        translate([+10,75,0]) cylinder(r=27, h=10, center=true); 
        translate([-15,130,0]) cylinder(r=35, h=10, center=true);
        translate([+25,120,0]) cylinder(r=25, h=10, center=true); 
    }

    hull()
    {
       
    }
}

module toes()
{
    translate ([-35,0,0]) scale([0.75,1,1]) cylinder(r=18, h=10, center=true);
    translate ([-5,-5,0]) scale([.5,.7,1]) cylinder(r=18, h=10, center=true);
    translate ([+18,-13,0]) rotate([0,0,-5]) scale([.5,.65,1]) cylinder(r=18, h=10, center=true);
    translate ([+37,-22,0]) rotate([0,0,-15]) scale([.45,.60,1]) cylinder(r=18, h=10, center=true);
    translate ([+54,-33,0]) rotate([0,0,-25]) scale([.4,.55,1]) cylinder(r=18, h=10, center=true);
}

module foot()
{
    minkowski()
    {
        sphere(r = 3.5, $fn = 8);
        translate([0,-90,0]) scale([.9,1,1])
        {
            base();
            translate([0, 190, 0]) toes();
        }
    }
}

foot(); 
