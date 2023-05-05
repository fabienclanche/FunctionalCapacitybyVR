module hmd()
{
	$fn = 20;
	difference()
	{
		translate ([0,50,0]) hull()
		{
			cube([200,90,90], center = true);
			cube([190,100,90], center = true);
			cube([190,90,100], center = true); 
		}
		
		translate ([33,0,0]) rotate([90,0,0]) cylinder(r=33, h=42, center = true);
		translate ([-33,0,0])rotate([90,0,0]) cylinder(r=33, h=42, center = true);
		translate ([0,0,33/2]) cube([66,42,33], center = true);
	}
	
	translate ([0,-65,-10]) difference()
	{
		union()
		{
			cylinder(r=125, h=28);
			rotate([0,90,0]) cylinder(r=122, h=36, center = true);
		}
		 
		rotate([0,180,0]) cylinder(r=125, h=250);
		sphere (r=115, $fn = 12);
	}
}

module tracker()
{
	$fn = 18;
	
	hull()
	{
		cylinder(r=38, r2=45, h = 12);
		translate([0,0,12]) cylinder(r=45, r2=45, h = 3);
		translate([0,0,15]) cylinder(r=45, r2=42, h = 3); 
	}
	
	for(i=[0:3]) rotate([0,0,i*120])
	{
		$fn = 8;
		translate([30.5,0,7]) scale([1,2,1]) rotate ([0,36,0]) cylinder(r=10, r2=8, h = 30);
	}
}

module controller()
{
	$fn = 18;
	
	difference()
	{
		union()
		{ 
			cylinder(r=50, h = 5, center = true);
			translate([0,0,+2.5]) cylinder(r=50, r2=40, h = 10.5);
			translate([0,0,-13.0]) cylinder(r=40, r2=50, h = 10.5);
			
			hull()
			{				
				$fn = 10;
				translate([0,-35,0]) scale([1.3,.75,1]) cylinder(r=20, h = 5, center = true);
				translate([0,-85,-120]) scale([1.1,.9,.65]) sphere(r=12.5);
			}
			
			for(i=[0:2]) rotate([0,0,i*180])
			{
				$fn = 8;
				translate([41,0,-5]) scale([1,2,1]) rotate ([0,140,0]) cylinder(r=9, r2=6, h = 17);
			}
		}
		
		translate([0,0,+2.5]) cylinder(r=25, r2=35, h = 11);  
		cylinder(r=25, h = 60, center = true);
	}
	 
	translate([0,-56,-40]) rotate([70,0,0]) cylinder(r=17, h = 20, center = true);
}

module lighthouse()
{
	translate ([0,0,0]) hull()
	{
		cube([100,78,90], center = true);
		translate ([0,-2.5,0]) cube([90,83,90], center = true);
		cube([90,78,100], center = true); 
	}
}

lighthouse();